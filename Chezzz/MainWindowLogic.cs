using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Chess;
using Path = System.IO.Path;

namespace Chezzz;

public partial class MainWindow
{
    private async Task WindowLoadedAsync()
    {
        const int margin = 10;

        Left = SystemParameters.WorkArea.Left + margin;
        Top = SystemParameters.WorkArea.Top + margin;
        Width = SystemParameters.WorkArea.Width - margin * 2;
        Height = SystemParameters.WorkArea.Height - margin * 2;
        Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - margin - Width) / 2;
        Top = SystemParameters.WorkArea.Top + (SystemParameters.WorkArea.Height - margin - Height) / 2;

        UpdateRequiredTime();
        UpdateRequiredScore();

        GotoPlatform();

        await Task.Run(ReadOpeningBook);
    }

    private async void GoAdvice()
    {
        if (!File.Exists(_stockfishPath)) {
            _status.Report($"{_stockfishPath} not found");
        }

        if (!Advice.IsEnabled) {
            return;
        }

        Advice.IsEnabled = false;
        await AdviceAsync();
        Advice.IsEnabled = true;
    }

    private static Models.Move GetMove(string rawline, ChessBoard board)
    {
        var move = new Models.Move();
        var parts = rawline.Split(' ');
        for (var i = 0; i < parts.Length; i++) {
            switch (parts[i]) {
                case "multipv":
                    move.Index = int.Parse(parts[i + 1], System.Globalization.CultureInfo.InvariantCulture) - 1;
                    break;
                case "mate":
                    var mateScore = int.Parse(parts[i + 1], System.Globalization.CultureInfo.InvariantCulture);
                    var score = new Models.Score(mateScore, true);
                    move.Score = score;
                    break;
                case "cp":
                    var moveScore = int.Parse(parts[i + 1], System.Globalization.CultureInfo.InvariantCulture);
                    score = new Models.Score(moveScore, false);
                    move.Score = score;
                    break;
                case "depth":
                    move.Depth = int.Parse(parts[i + 1], System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "wdl":
                    var wdl = new int[3];
                    wdl[0] = int.Parse(parts[i + 1], System.Globalization.CultureInfo.InvariantCulture);
                    wdl[1] = int.Parse(parts[i + 2], System.Globalization.CultureInfo.InvariantCulture);
                    wdl[2] = int.Parse(parts[i + 3], System.Globalization.CultureInfo.InvariantCulture);
                    var iwdl = Array.IndexOf(wdl, wdl.Max());
                    var twdl = iwdl switch {
                        0 => "win",
                        1 => "draw",
                        2 => "lose",
                        _ => "?"
                    };

                    move.Forecast = wdl[iwdl] >= 995 ? twdl : $"{twdl} {Math.Round(wdl[iwdl] / 10.0):F0}%";
                    break;
                case "pv":
                    move.FirstMove = parts[i + 1];
                    move.FirstPiece = GetPiece(move.FirstMove, board);
                    if (i + 2 < parts.Length) {
                        move.SecondMove = parts[i + 2];
                        move.SecondPiece = GetPiece(move.SecondMove, board);
                    }
                    break;
            }
        }

        return move;
    }

    private async Task AdviceAsync()
    {
        const string script = "document.documentElement.outerHTML";
        var result = await WebBrowser.CoreWebView2.ExecuteScriptAsync(script);
        var decodedHtml = Regex.Unescape(result.Trim('"'));
#if DEBUG
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        await File.WriteAllTextAsync(Path.Combine(appDirectory, "decodedHtml.html"), decodedHtml);
#endif
        var requiredTime = _requiredTime.GetValue();
        var error = string.Empty;
        var currentFen = string.Empty;
        var previousFen = string.Empty;
        var previousMove = string.Empty;
        var board = new ChessBoard();
        _isWhite = true;
        switch (Platform.SelectionBoxItem) {
            case AppConsts.CHESS:
                GetFenFromChess(decodedHtml, out error, out board, out previousFen, out previousMove, out currentFen);
                break;
            case AppConsts.LICHESS:
                GetFenFromLiChess(decodedHtml, out error, out board, out previousFen, out previousMove, out currentFen);
                break;
        }

        if (!string.IsNullOrEmpty(error)) {
            _status.Report(error);
            return;
        }

        Fen.Text = currentFen;

        var stockfish = new Process[2];
        var inputWriter = new TextWriter[2];
        var outputReader = new TextReader[2];

        var playerDone = false;
        var opponentDone = false;

        _moves.Clear();
        _opponentMoves.Clear();
        _opponentArrow = string.Empty;
        _currentScore = null;

        for (var i = 0; i < 2; i++) {
            stockfish[i] = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = _stockfishPath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            stockfish[i].Start();

            inputWriter[i] = stockfish[i].StandardInput;
            outputReader[i] = stockfish[i].StandardOutput;

            await inputWriter[i].WriteLineAsync("uci");
            await inputWriter[i].FlushAsync();

            while (await outputReader[i].ReadLineAsync() is { } line) {
                if (line.StartsWith("uciok", StringComparison.Ordinal))
                    break;
            }

            await inputWriter[i].WriteLineAsync("setoption name UCI_LimitStrength value false");
            await inputWriter[i].FlushAsync();

            await inputWriter[i].WriteLineAsync("setoption name Threads value 16");
            await inputWriter[i].FlushAsync();

            await inputWriter[i].WriteLineAsync("setoption name UCI_ShowWDL value true");
            await inputWriter[i].FlushAsync();

            await inputWriter[i].WriteLineAsync("setoption name MultiPV value 256");
            await inputWriter[i].FlushAsync();

            await inputWriter[i].WriteLineAsync("ucinewgame");
            await inputWriter[i].FlushAsync();
        }

        await inputWriter[0].WriteLineAsync($"position fen {currentFen}");
        await inputWriter[0].FlushAsync();

        await inputWriter[0].WriteLineAsync("isready");
        await inputWriter[0].FlushAsync();

        
        while (await outputReader[0].ReadLineAsync() is { } rawline ) {
            if (rawline.StartsWith("readyok", StringComparison.Ordinal)) {
                break;
            }
        }

        await inputWriter[0].WriteLineAsync($"go movetime {requiredTime}");
        await inputWriter[0].FlushAsync();

        if (!string.IsNullOrEmpty(previousFen) && !string.IsNullOrEmpty(previousMove)) {
            await inputWriter[1].WriteLineAsync($"position fen {previousFen}");
            await inputWriter[1].FlushAsync();

            await inputWriter[1].WriteLineAsync("isready");
            await inputWriter[1].FlushAsync();

            while (await outputReader[1].ReadLineAsync() is { } rawline) {
                if (rawline.StartsWith("readyok", StringComparison.Ordinal)) {
                    break;
                }
            }

            await inputWriter[1].WriteLineAsync($"go movetime {requiredTime}");
            await inputWriter[1].FlushAsync();
        }
        else {
            opponentDone = true;
        }

        while (!playerDone || !opponentDone) {
            if (!opponentDone) {
                var line = await outputReader[1].ReadLineAsync();
                if (line is not null) {
                    if (line.StartsWith("bestmove", StringComparison.Ordinal)) {
                        opponentDone = true;
                    }
                    else {
                        if (line.Contains(" multipv ")) {
                            var move = GetMove(line, board);
                            if (_opponentMoves.GetCount() > 0 && move.Depth > _opponentMoves.GetDepth()) {
                                _currentScore = _opponentMoves.GetScoreOpponent();
                                _opponentArrow = _opponentMoves.GetArrowOpponent(previousMove, _isWhite);
                                await AddArrowPlayer();
                                _opponentMoves.Clear();
                            }

                            _opponentMoves.Add(move);
                        }
                    }
                }
            }

            if (!playerDone) {
                var line = await outputReader[0].ReadLineAsync();
                if (line is not null) {
                    if (line.StartsWith("bestmove", StringComparison.Ordinal)) {
                        playerDone = true;
                    }
                    else {
                        _status?.Report(line);
                        if (line.Contains(" multipv ")) {
                            var move = GetMove(line, board);
                            if (_moves.GetCount() > 0 && move.Depth > _moves.GetDepth()) {
                                _moves.Clear();
                            }

                            _moves.Add(move);
                        }
                    }
                }
            }
        }

        _currentScore ??= _moves.GetScore();

        foreach (var move in _moves.GetMoves()) {
            await inputWriter[0].WriteLineAsync($"position fen {currentFen} moves {move.FirstMove}");
            await inputWriter[0].FlushAsync();

            await inputWriter[0].WriteLineAsync("d");
            await inputWriter[0].FlushAsync();

            var newFen = string.Empty;
            while (await outputReader[0].ReadLineAsync() is { } rawline) {
                if (!rawline.StartsWith("Fen:", StringComparison.Ordinal)) {
                    continue;
                }

                newFen = rawline[5..].Trim();
                break;
            }

            var pos = newFen.IndexOf(' ');
            if (pos > 0) {
                newFen = newFen[..pos];
            }

            if (!string.IsNullOrEmpty(newFen)) {
                if (_openings.TryGetValue(newFen, out var opening)) {
                    move.Opening = opening;
                }
            }
        }

        ShowMoves(true);
        await AddArrowPlayer();

        for (var i = 0; i < 2; i++) {
            await inputWriter[i].WriteLineAsync("quit");
            await inputWriter[i].FlushAsync();

            await stockfish[i].WaitForExitAsync();
            stockfish[i].Close();
        }

        var bestMove = _moves.GetFirstMove();
        _status?.Report($"Done. The best move: {bestMove.FirstPiece}{bestMove.FirstMove}; score: {bestMove.Score} (depth: {bestMove.Depth})");
    }

    private void ChangeRequiredScore(int delta)
    {
        _requiredScore.ChangeValue(delta);
        UpdateRequiredScore();
    }

    private void ChangeRequiredTime(int delta)
    {
        _requiredTime.ChangeValue(delta);
        UpdateRequiredTime();
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++) {
            var c = line[i];
            if (inQuotes) {
                if (c == '"') {
                    if (i + 1 < line.Length && line[i + 1] == '"') {
                        field.Append('"');
                        i++;
                    }
                    else {
                        inQuotes = false;
                    }
                }
                else {
                    field.Append(c);
                }
            }
            else {
                switch (c) {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        fields.Add(field.ToString());
                        field.Clear();
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }
        }

        fields.Add(field.ToString());
        return fields;
    }

    private async Task ReadOpeningBook()
    {
        var bookPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "openings.zip");
        if (!File.Exists(bookPath)) {
            return;
        }

        _status.Report("Reading named book moves...");

        var zipBytes = await File.ReadAllBytesAsync(bookPath).ConfigureAwait(false);
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var entry = archive.GetEntry("openings.csv");
        if (entry != null) {
            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            var text = await reader.ReadToEndAsync().ConfigureAwait(false);
            var lines = text.Split("\r\n");
            foreach (var line in lines) {
                var fields = ParseCsvLine(line);
                if (fields.Count != 2) {
                    continue;
                }

                _openings[fields[0]] = fields[1];
            }
        }

        _status.Report("Done");
    }
}