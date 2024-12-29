using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;

namespace Chezzz;

public partial class MainWindow
{
    private IProgress<string>? _status;
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

                var rankNumber = int.Parse(square.Substring(0, 1));
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

            // Final evaluation       +0.47 (white side) [with scaled NNUE, ...]
            // Final evaluation: none (in check)

            await inputWriter.WriteLineAsync("eval");
            await inputWriter.FlushAsync();

            var eval = "?";
            while ((line = await outputReader.ReadLineAsync()) != null) {
                if (line.StartsWith("Final evaluation")) {
                    var pars = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (pars.Length >= 2) {
                        eval = pars[2];
                        break;
                    }
                }
            }

            await inputWriter.WriteLineAsync($"go depth {_depth}");
            await inputWriter.FlushAsync();

            while ((line = await outputReader.ReadLineAsync()) != null) {
                string bestMove = string.Empty;
                if (line.StartsWith("bestmove")) {
                    var parts = line.Split(' ');
                    if (parts.Length >= 2) {
                        bestMove = parts[1];
                    }

                    _status?.Report(!string.IsNullOrEmpty(bestMove)
                        ? $"[{eval}] Best move: {bestMove}"
                        : $"[{eval}] Cannot find the best move");

                    inputWriter.Close();
                    stockfish.Close();
                    return;
                }

                _status?.Report(line);
            }
        }
    }
}