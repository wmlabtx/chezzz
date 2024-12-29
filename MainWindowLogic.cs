using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Shapes;

namespace Chezzz;

public partial class MainWindow : Window
{
    private IProgress<string>? _status;
    private readonly string? _stockfishPath;
    private readonly string? _elo;
    private readonly string? _depth;
    private readonly string? _threads;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isRunning = false;

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

    private async void StartBackgroundProcess()
    {
        const string script = "document.documentElement.outerHTML";
        var result = await webView2.CoreWebView2.ExecuteScriptAsync(script);
        var decodedHtml = Regex.Unescape(result.Trim('"'));

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        await Task.Run(() => BackgroundWork(decodedHtml, token), token);
        _isRunning = true;
    }

    private void StopBackgroundProcess()
    {
        _cancellationTokenSource.Cancel();
        _isRunning = false;
    }

    private async void BackgroundWork(string decodedHtml, CancellationToken token)
    {
        Process? stockfish = null;
        StreamWriter? inputWriter = null;
        StreamReader? outputReader = null;

        try {
            var isWhite = true;
            var match = ChessBoardRegex().Match(decodedHtml);
            if (match.Success) {
                var classValue = match.Groups[1].Value;
                if (classValue.Contains("flipped")) {
                    isWhite = false;
                }
            }
            else {
                Console.WriteLine("<wc-chess-board> not found");
            }

            var blockMatch = BlockRegex().Match(decodedHtml);
            if (!blockMatch.Success) {
                Console.WriteLine("The block between <!--/Effects--> and <!--/Pieces--> not found");
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

                stockfish = new Process {
                    StartInfo = new ProcessStartInfo {
                        FileName = _stockfishPath,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                stockfish.Start();

                inputWriter = stockfish.StandardInput;
                outputReader = stockfish.StandardOutput;

                await inputWriter.WriteLineAsync("uci");
                await inputWriter.FlushAsync(token);

                string? line;
                while ((line = await outputReader.ReadLineAsync(token)) != null) {
                    if (line.StartsWith("uciok"))
                        break;
                }

                await inputWriter.WriteLineAsync("setoption name UCI_LimitStrength value true");
                await inputWriter.FlushAsync(token);

                await inputWriter.WriteLineAsync($"setoption name UCI_Elo value {_elo}");
                await inputWriter.FlushAsync(token);

                await inputWriter.WriteLineAsync($"setoption name Threads value {_threads}");
                await inputWriter.FlushAsync(token);

                await inputWriter.WriteLineAsync("ucinewgame");
                await inputWriter.FlushAsync(token);

                await inputWriter.WriteLineAsync($"position fen {fen}");
                await inputWriter.FlushAsync(token);

                await inputWriter.WriteLineAsync("isready");
                await inputWriter.FlushAsync(token);

                while ((line = await outputReader.ReadLineAsync(token)) != null) {
                    if (line.StartsWith("readyok"))
                        break;
                }

                await inputWriter.WriteLineAsync($"go depth {_depth}");
                await inputWriter.FlushAsync(token);

                while ((line = await outputReader.ReadLineAsync(token)) != null) {
                    string bestMove = string.Empty;
                    if (line.StartsWith("bestmove")) {
                        var parts = line.Split(' ');
                        if (parts.Length >= 2) {
                            bestMove = parts[1];
                        }

                        if (!string.IsNullOrEmpty(bestMove)) {
                            _status?.Report("Best move: " + bestMove);
                        }
                        else {
                            _status?.Report("cannot find the best move");
                        }

                        inputWriter.Close();
                        stockfish.Close();
                        Dispatcher.Invoke(() => { _isRunning = false; });
                        return;
                    }

                    // info depth 1 seldepth 2 multipv 1 score cp 18 nodes 860 nps 860000 hashfull 0 tbhits 0 time 1 pv f1c4
                    // info depth 33 currmove b1c3 currmovenumber 2
                    var pars = line.Split(' ');
                    var depth = "?";
                    var pv = "?";
                    var score = "max";
                    if (pars[0].Equals("info")) {
                        var index = Array.IndexOf(pars, "depth");
                        if (index >= 0) {
                            depth = pars[index + 1];
                        }

                        index = Array.IndexOf(pars, "cp");
                        if (index >= 0) {
                            score = pars[index + 1];
                        }

                        index = Array.IndexOf(pars, "pv");
                        if (index >= 0) {
                            pv = pars[index + 1];
                        }

                        index = Array.IndexOf(pars, "currmove");
                        if (index >= 0) {
                            pv = pars[index + 1];
                        }

                        line = $"{pv} ({score}) {depth}";
                    }

                    _status?.Report(line);
                    token.ThrowIfCancellationRequested();
                }
            }
        }
        catch (OperationCanceledException) {
            if (stockfish != null && inputWriter != null && outputReader != null) {
                inputWriter.WriteLine("stop");
                inputWriter.Flush();

                var bestMove = string.Empty;
                string? line;
                while ((line = outputReader.ReadLine()) != null) {
                    if (line.StartsWith("bestmove")) {
                        var parts = line.Split(' ');
                        if (parts.Length >= 2) {
                            bestMove = parts[1];
                        }

                        break;
                    }
                }

                if (!string.IsNullOrEmpty(bestMove)) {
                    _status?.Report("Best move: " + bestMove);
                }
                else {
                    _status?.Report("cannot find the best move");
                }

                inputWriter.Close();
                stockfish.Close();
            }
        }
        finally {
            Dispatcher.Invoke(() => { _isRunning = false; });
        }
    }

}