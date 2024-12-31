using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Chezzz;

public partial class MainWindow
{
    private IProgress<string>? _status;
    private Border[] _scoreBorder;
    private Border[] _wdlBorder;
    private Border[] _bestMoveBorder;
    private Border[] _moveBorder;
    private Label[] _scoreText;
    private Label[] _wdlText;
    private Label[] _bestMoveText;
    private Label[] _moveText;

    private string? _stockfishPath;
    private string? _elo;
    private string? _depth;
    private string? _threads;

    private void WindowLoaded()
    {
        const int margin = 10;

        Left = SystemParameters.WorkArea.Left + margin;
        Top = SystemParameters.WorkArea.Top + margin;
        Width = SystemParameters.WorkArea.Width - margin * 2;
        Height = SystemParameters.WorkArea.Height - margin * 2;
        Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - margin - Width) / 2;
        Top = SystemParameters.WorkArea.Top + (SystemParameters.WorkArea.Height - margin - Height) / 2;

        _status = new Progress<string>(message => Status.Text = message);
        _scoreBorder = new[] { ScoreBorder1, ScoreBorder2, ScoreBorder3 };
        _wdlBorder = new[] { WdlBorder1, WdlBorder2, WdlBorder3 };
        _bestMoveBorder = new[] { BestMoveBorder1, BestMoveBorder2, BestMoveBorder3 };
        _moveBorder = new[] { MoveBorder1, MoveBorder2, MoveBorder3 };
        _scoreText = new[] { ScoreText1, ScoreText2, ScoreText3 };
        _wdlText = new[] { WdlText1, WdlText2, WdlText3 };
        _bestMoveText = new[] { BestMoveText1, BestMoveText2, BestMoveText3 };
        _moveText = new[] { MoveText1, MoveText2, MoveText3 };
    }

    [GeneratedRegex(@"<wc-chess-board[^>]*\bclass\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex ChessBoardRegex();

    [GeneratedRegex(@"<!--/Effects-->(?<block>[\s\S]*?)<!--/Pieces-->", RegexOptions.IgnoreCase)]
    private static partial Regex BlockRegex();

    [GeneratedRegex(@"<div[^>]*\bclass\s*=\s*""(piece[^""]*)""[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DivRegex();

    private async Task AdviceAsync()
    {
        const string script = "document.documentElement.outerHTML";
        var result = await webView2.CoreWebView2.ExecuteScriptAsync(script);
        var decodedHtml = Regex.Unescape(result.Trim('"'));

        var isWhite = true;
        var match = ChessBoardRegex().Match(decodedHtml);
        if (match.Success) {
            var classValue = match.Groups[1].Value;
            if (classValue.Contains("flipped")) {
                isWhite = false;
            }
        }
        else {
            _status?.Report("<wc-chess-board> not found");
        }

        var blockMatch = BlockRegex().Match(decodedHtml);
        if (!blockMatch.Success) {
            _status?.Report("The block between <!--/Effects--> and <!--/Pieces--> not found");
            return;
        }

        var blockContent = blockMatch.Groups["block"].Value;
        var matches = DivRegex().Matches(blockContent);

        var board = new char[8, 8];

        for (var r = 0; r < 8; r++)
        for (var c = 0; c < 8; c++)
            board[r, c] = '.';

        if (matches.Count == 0) {
            _status?.Report("<div> not found");
        }
        else {
            foreach (Match m in matches) {
                var classValue = m.Groups[1].Value;
                var pars = classValue.Split(' ');
                if (pars.Length != 3) {
                    _status?.Report("incorrect <div>");
                    continue;
                }

                var color = '.';
                var piece = '.';
                var square = "00";
                foreach (var par in pars) {
                    if (par.Length == 2) {
                        color = par[0];
                        piece = par[1];
                    }
                    else if (par.StartsWith("square-")) {
                        square = par.Substring(7, 2);
                    }
                }

                var rankNumber = int.Parse(square[..1]);
                var col = rankNumber - 1;

                rankNumber = int.Parse(square.Substring(1, 1));
                var row = 8 - rankNumber;

                var pieceChar = piece;
                pieceChar = color == 'b' ? char.ToLower(pieceChar) : char.ToUpper(pieceChar);

                board[row, col] = pieceChar;
            }

            var fenParts = new string[8];

            for (var row = 0; row < 8; row++) {
                var emptyCount = 0;
                var rowFen = "";
                for (var col = 0; col < 8; col++) {
                    var piece = board[row, col];
                    if (piece == '.') {
                        emptyCount++;
                    }
                    else {
                        if (emptyCount > 0) {
                            rowFen += emptyCount.ToString();
                            emptyCount = 0;
                        }

                        rowFen += piece;
                    }
                }

                if (emptyCount > 0) {
                    rowFen += emptyCount.ToString();
                }

                fenParts[row] = rowFen;
            }

            var ranksFen = string.Join("/", fenParts);

            var turn = isWhite ? "w" : "b";
            var castling = string.Empty;
            if (board[7, 0] == 'R' && board[7, 4] == 'K') {
                castling += "K";
            }

            if (board[7, 7] == 'R' && board[7, 4] == 'K') {
                castling += "Q";
            }

            if (board[0, 0] == 'r' && board[0, 4] == 'k') {
                castling += "k";
            }

            if (board[0, 7] == 'r' && board[0, 4] == 'k') {
                castling += "q";
            }

            if (string.IsNullOrEmpty(castling)) {
                castling = "-";
            }

            var fen = ranksFen + $" {turn} {castling} - 0 1";

            var stockfish = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = _stockfishPath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            stockfish.Start();

            var inputWriter = stockfish.StandardInput;
            var outputReader = stockfish.StandardOutput;

            await inputWriter.WriteLineAsync("uci");
            await inputWriter.FlushAsync();

            string? line;
            while ((line = await outputReader.ReadLineAsync()) != null) {
                if (line.StartsWith("uciok"))
                    break;
            }

            await inputWriter.WriteLineAsync("setoption name UCI_LimitStrength value true");
            await inputWriter.FlushAsync();

            await inputWriter.WriteLineAsync($"setoption name UCI_Elo value {_elo}");
            await inputWriter.FlushAsync();

            await inputWriter.WriteLineAsync($"setoption name Threads value {_threads}");
            await inputWriter.FlushAsync();

            await inputWriter.WriteLineAsync("setoption name UCI_ShowWDL value true");
            await inputWriter.FlushAsync();

            await inputWriter.WriteLineAsync("setoption name MultiPV value 3");
            await inputWriter.FlushAsync();

            await inputWriter.WriteLineAsync("ucinewgame");
            await inputWriter.FlushAsync();

            await inputWriter.WriteLineAsync($"position fen {fen}");
            await inputWriter.FlushAsync();

            await inputWriter.WriteLineAsync("isready");
            await inputWriter.FlushAsync();

            while ((line = await outputReader.ReadLineAsync()) != null) {
                if (line.StartsWith("readyok"))
                    break;
            }

            await inputWriter.WriteLineAsync($"go depth {_depth}");
            await inputWriter.FlushAsync();

            while ((line = await outputReader.ReadLineAsync()) != null) {
                _status?.Report(line);
                if (line.StartsWith("bestmove")) {
                    break;
                }

                if (line.IndexOf(" multipv ", StringComparison.Ordinal) < 0) {
                    continue;
                }

                var score = string.Empty;
                var bestMove = string.Empty;
                var move = string.Empty;
                var win = string.Empty;
                var wdl = new int[3];
                SolidColorBrush colorScore;
                var colorWdl = Brushes.Black;
                var sb = new StringBuilder();
                var parts = line.Split(' ');
                var pvIndex = 0;
                for (var i = 0; i < parts.Length; i++) {
                    if (parts[i].Equals("multipv")) {
                        pvIndex = int.Parse(parts[i + 1]) - 1;
                        continue;
                    }

                    if (parts[i].Equals("mate")) {
                        score = parts[i + 1].StartsWith('-') ? $"-M{parts[i + 1][1..]}" : $"+M{parts[i + 1]}";
                        continue;
                    }

                    if (parts[i].Equals("cp")) {
                        var cp = int.Parse(parts[i + 1]);
                        score = cp < 0 ? $"-{Math.Abs(cp) / 100.0:F2}" : $"+{cp / 100.0:F2}";
                        continue;
                    }

                    if (parts[i].Equals("wdl")) {
                        wdl[0] = int.Parse(parts[i + 1]);
                        wdl[1] = int.Parse(parts[i + 2]);
                        wdl[2] = int.Parse(parts[i + 3]);
                        var iwdl = Array.IndexOf(wdl, wdl.Max());
                        colorWdl = iwdl switch {
                            0 => Brushes.Green,
                            1 => Brushes.Gray,
                            2 => Brushes.Red,
                            _ => Brushes.Black
                        };

                        var twdl = iwdl switch {
                            0 => "win",
                            1 => "draw",
                            2 => "lose",
                            _ => "?"
                        };

                        win = wdl[iwdl] >= 995 ? twdl : $"{twdl} {Math.Round(wdl[iwdl] / 10.0):F0}%";

                    }

                    if (parts[i].Equals("pv")) {
                        bestMove = parts[i + 1];
                        if (i + 2 < parts.Length) {
                            for (var j = i + 2; j < parts.Length; j++) {
                                sb.Append(parts[j]);
                                sb.Append(' ');
                            }
                        }

                        move = sb.ToString();
                    }
                }

                if (score[1] == '0') {
                    colorScore = Brushes.Gray;
                }
                else if (score[0] == '-') {
                    colorScore = Brushes.Red;
                }
                else {
                    colorScore = Brushes.Green;
                }

                if (string.IsNullOrEmpty(score)) {
                    _scoreBorder[pvIndex].Visibility = Visibility.Collapsed;
                    _wdlBorder[pvIndex].Visibility = Visibility.Collapsed;
                    _bestMoveBorder[pvIndex].Visibility = Visibility.Collapsed;
                    _moveBorder[pvIndex].Visibility = Visibility.Collapsed;
                }
                else {
                    _scoreBorder[pvIndex].Background = colorScore;
                    _scoreText[pvIndex].Content = score;
                    _scoreBorder[pvIndex].Visibility = Visibility.Visible;

                    _wdlBorder[pvIndex].Background = colorWdl;
                    _wdlText[pvIndex].Content = win;
                    _wdlBorder[pvIndex].Visibility = Visibility.Visible;

                    _bestMoveText[pvIndex].Content = bestMove;
                    _bestMoveBorder[pvIndex].Visibility = Visibility.Visible;

                    _moveText[pvIndex].Content = move;
                    _moveBorder[pvIndex].Visibility = Visibility.Visible;
                }
            }

            _status?.Report("Done");
            inputWriter.Close();
            stockfish.Close();
        }
    }
}