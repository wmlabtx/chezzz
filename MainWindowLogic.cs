using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Chezzz;

public partial class MainWindow
{
    private readonly IProgress<string>? _status;
    private readonly Border[] _scoreBorder;
    private readonly Border[] _wdlBorder;
    private readonly Border[] _bestMoveBorder;
    private readonly Border[] _moveBorder;
    private readonly Label[] _scoreText;
    private readonly Label[] _wdlText;
    private readonly Label[] _bestMoveText;
    private readonly Label[] _moveText;

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

        GotoPlatform();
    }

    [GeneratedRegex(@"<wc-chess-board[^>]*\bclass\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex ChessBoardRegex();

    [GeneratedRegex(@"<!--/Effects-->(?<block>[\s\S]*?)<!--/Pieces-->", RegexOptions.IgnoreCase)]
    private static partial Regex BlockRegex();

    [GeneratedRegex(@"<div[^>]*\bclass\s*=\s*""(piece[^""]*)""[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DivRegex();

    [GeneratedRegex(@"<div\s+class=""cg-wrap\s+orientation-(?<orientation>\w+)\s+manipulable""><cg-container\s+style=""width:\s*(?<width>\d+)px;\s*height:\s*(?<height>\d+)px;"">", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex OrientationRegex();

    [GeneratedRegex(@"<cg-board>(?<content>[\s\S]*?)<\/cg-board>", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex CgBoardRegex();

    [GeneratedRegex(@"<piece\s+class\s*=\s*""(?<CCC>\w+)\s+(?<FFF>\w+)""\s+style\s*=\s*""transform:\s*translate\(\s*(?<XXX>\d+)px\s*,\s*(?<YYY>\d+)px\s*\);?""\s*><\/piece>", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex PieceRegex();

    private static string GetFen(char[,] board, bool isWhite)
    {
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
        return fen;
    }

    private void GetFenFromChess(string decodedHtml, out string error, out string fen)
    {
        error = string.Empty;
        fen = string.Empty;
        var isWhite = true;
        var match = ChessBoardRegex().Match(decodedHtml);
        if (match.Success) {
            var classValue = match.Groups[1].Value;
            if (classValue.Contains("flipped")) {
                isWhite = false;
            }
        }
        else {
            error = "<wc-chess-board> not found";
            return;
        }

        var blockMatch = BlockRegex().Match(decodedHtml);
        if (!blockMatch.Success) {
            error = "The block between <!--/Effects--> and <!--/Pieces--> not found";
            return;
        }

        var blockContent = blockMatch.Groups["block"].Value;
        var matches = DivRegex().Matches(blockContent);

        var board = new char[8, 8];

        for (var r = 0; r < 8; r++)
            for (var c = 0; c < 8; c++)
                board[r, c] = '.';

        if (matches.Count == 0) {
            error = "<div> not found";
            return;
        }
        
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

        fen = GetFen(board, isWhite);
    }

    private static void GetFenFromLiChess(string decodedHtml, out string error, out string fen)
    {
        error = string.Empty;
        fen = string.Empty;

        // <div class="cg-wrap orientation-white manipulable"><cg-container style="width: 736px; height: 736px;">
        // <div class="cg-wrap orientation-black manipulable"><cg-container style="width: 736px; height: 736px;">
        var isWhite = true;
        var regex = OrientationRegex();
        var m = regex.Match(decodedHtml);
        if (!m.Success) {
            error = "cg-wrap orientation not found";
            return;
        }

        var orientation = m.Groups["orientation"].Value;
        if (orientation.Equals("black")) {
            isWhite = false;
        }
        else {
            if (!orientation.Equals("white")) {
                error = $"Unknown orientation '{orientation}'";
                return;
            }
        }

        var strwidth = m.Groups["width"].Value;
        var width = int.Parse(strwidth);
        var strheight = m.Groups["height"].Value;
        var height = int.Parse(strheight);
        var squareX = width / 8.0;
        var squareY = height / 8.0;

        regex = CgBoardRegex();
        var matches = regex.Matches(decodedHtml);
        if (matches.Count == 0) {
            error = "Block between <cg-board> and </cg-board> not found";
            return;
        }

        var blockContent = matches[0].Groups["content"].Value;
        regex = PieceRegex();
        matches = regex.Matches(blockContent);
        if (matches.Count == 0) {
            error = "<piece> not found";
            return;
        }

        var board = new char[8, 8];
        for (var r = 0; r < 8; r++)
            for (var c = 0; c < 8; c++)
                board[r, c] = '.';

        foreach (Match match in matches) {
            // <piece class="white pawn" style="transform: translate(460px, 552px);"></piece>
            var rawColor = match.Groups["CCC"].Value; // 
            var rawPiece = match.Groups["FFF"].Value;
            var rawX = match.Groups["XXX"].Value;
            var rawY = match.Groups["YYY"].Value;

            var color = rawColor[0];
            var piece = rawPiece switch {
                "pawn" => 'p',
                "knight" => 'n',
                "bishop" => 'b',
                "rook" => 'r',
                "queen" => 'q',
                "king" => 'k',
                _ => '.'
            };

            
            var col = (int)Math.Round(int.Parse(rawX) / squareX);
            var row = (int)Math.Round(int.Parse(rawY) / squareY);
            if (!isWhite) {
                col = 7 - col;
                row = 7 - row;
            }

            var pieceChar = piece;
            pieceChar = color == 'b' ? char.ToLower(pieceChar) : char.ToUpper(pieceChar);

            board[row, col] = pieceChar;
        }

        fen = GetFen(board, isWhite);
    }

    private async Task AdviceAsync()
    {
        const string script = "document.documentElement.outerHTML";
        var result = await WebBrowser.CoreWebView2.ExecuteScriptAsync(script);
        var decodedHtml = Regex.Unescape(result.Trim('"'));

        var error = string.Empty;
        var fen = string.Empty;
        switch (Platform.SelectionBoxItem) {
            case AppConsts.CHESS:
                GetFenFromChess(decodedHtml, out error, out fen);
                break;
            case AppConsts.LICHESS:
                GetFenFromLiChess(decodedHtml, out error, out fen);
                break;
        }

        if (!string.IsNullOrEmpty(error)) {
            _status?.Report(error);
            return;
        }

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

        await inputWriter.WriteLineAsync("setoption name MultiPV value 256");
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

        var moves = new SortedList<string, Move>();
        var status = string.Empty;
        while ((line = await outputReader.ReadLineAsync()) != null) {
            _status?.Report(line);
            string[] parts;
            if (line.StartsWith("bestmove")) {
                parts = line.Split(' ');
                if (parts.Length > 1) {
                    var suggestedMove = parts[1];
                    status = moves.ContainsKey(suggestedMove) ? 
                        $"[{_elo}] suggests {suggestedMove} | {moves[suggestedMove].Score} | {moves[suggestedMove].Forecast}" : 
                        $"[{_elo}] suggests {suggestedMove}";
                }

                break;
            }

            if (line.IndexOf(" multipv ", StringComparison.Ordinal) < 0) {
                continue;
            }

            var move = new Move();
            parts = line.Split(' ');
            for (var i = 0; i < parts.Length; i++) {
                if (parts[i].Equals("multipv")) {
                    move.Index = int.Parse(parts[i + 1]) - 1;
                    continue;
                }

                if (parts[i].Equals("mate")) {
                    move.Score = parts[i + 1].StartsWith('-') ? $"-M{parts[i + 1][1..]}" : $"+M{parts[i + 1]}";
                    continue;
                }

                if (parts[i].Equals("cp")) {
                    var cp = int.Parse(parts[i + 1]);
                    move.Score = cp < 0 ? $"-{Math.Abs(cp) / 100.0:F2}" : $"+{cp / 100.0:F2}";
                    continue;
                }

                if (parts[i].Equals("depth")) {
                    move.Depth = int.Parse(parts[i + 1]);
                    continue;
                }

                if (parts[i].Equals("wdl")) {
                    var wdl = new int[3];
                    wdl[0] = int.Parse(parts[i + 1]);
                    wdl[1] = int.Parse(parts[i + 2]);
                    wdl[2] = int.Parse(parts[i + 3]);
                    var iwdl = Array.IndexOf(wdl, wdl.Max());
                    move.Color = iwdl switch {
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

                    move.Forecast = wdl[iwdl] >= 995 ? twdl : $"{twdl} {Math.Round(wdl[iwdl] / 10.0):F0}%";
                }

                if (parts[i].Equals("pv")) {
                    move.FirstMove = parts[i + 1];
                    var sb = new StringBuilder();
                    if (i + 2 < parts.Length) {
                        for (var j = i + 2; j < parts.Length; j++) {
                            sb.Append(parts[j]);
                            sb.Append(' ');
                        }
                    }

                    move.Moves = sb.ToString();
                }
            }

            if (moves.Count > 0 && move.Depth > moves.Values[0].Depth) {
                moves.Clear();
            }

            moves[move.FirstMove] = move;
            if (move.Index < 3) {
                _scoreBorder[move.Index].Background = move.Color;
                _scoreText[move.Index].Content = move.Score;
                _scoreBorder[move.Index].Visibility = Visibility.Visible;

                _wdlBorder[move.Index].Background = move.Color;
                _wdlText[move.Index].Content = move.Forecast;
                _wdlBorder[move.Index].Visibility = Visibility.Visible;

                _bestMoveText[move.Index].Content = move.FirstMove;
                _bestMoveBorder[move.Index].Visibility = Visibility.Visible;

                _moveText[move.Index].Content = move.Moves;
                _moveBorder[move.Index].Visibility = Visibility.Visible;
            }
        }

        if (moves.Count < 3) {
            for (var i = moves.Count; i < 3; i++) {
                _scoreBorder[i].Visibility = Visibility.Hidden;
                _wdlBorder[i].Visibility = Visibility.Hidden;
                _bestMoveBorder[i].Visibility = Visibility.Hidden;
                _moveBorder[i].Visibility = Visibility.Hidden;
            }
        }

        _status?.Report(status);

        inputWriter.Close();
        stockfish.Close();
    }
}