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

    private static Move GetMove(string rawline, ChessBoard board)
    {
        var move = new Move();
        var parts = rawline.Split(' ');
        for (var i = 0; i < parts.Length; i++) {
            switch (parts[i]) {
                case "multipv":
                    move.Index = int.Parse(parts[i + 1]) - 1;
                    break;
                case "mate":
                    move.Score = parts[i + 1].StartsWith('-') ? NEGATIVE_MATE : POSITIVE_MATE;
                    var mateScore = 20 - Math.Min(20, Math.Abs(int.Parse(parts[i + 1])));
                    if (move.Score > 0) {
                        move.Score += mateScore;
                    }
                    else {
                        move.Score -= mateScore;
                    }

                    move.ScoreText = parts[i + 1].StartsWith('-') ? $"-M{parts[i + 1][1..]}" : $"+M{parts[i + 1]}";
                    break;
                case "cp":
                    move.Score = int.Parse(parts[i + 1]);
                    move.ScoreText = move.Score < 0 ? $"-{Math.Abs(move.Score) / 100.0:F2}" : $"+{move.Score / 100.0:F2}";
                    break;
                case "depth":
                    move.Depth = int.Parse(parts[i + 1]);
                    break;
                case "wdl":
                    var wdl = new int[3];
                    wdl[0] = int.Parse(parts[i + 1]);
                    wdl[1] = int.Parse(parts[i + 2]);
                    wdl[2] = int.Parse(parts[i + 3]);
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
        await File.WriteAllTextAsync(  Path.Combine(appDirectory, "decodedHtml.html"), decodedHtml);
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
        SortedList<int, Move> opponentMoves = new();
        _selectedIndex = -1;
        _opponentArrow = string.Empty;

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
                if (line.StartsWith("uciok"))
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
            if (rawline.StartsWith("readyok")) {
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
                if (rawline.StartsWith("readyok")) {
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
                    if (line.StartsWith("bestmove")) {
                        opponentDone = true;
                    }
                    else {
                        if (line.IndexOf(" multipv ", StringComparison.Ordinal) >= 0) {
                            var move = GetMove(line, board);
                            if (opponentMoves.Count > 0 && move.Depth > opponentMoves.Values[0].Depth) {
                                var index = -1;
                                foreach (var m in opponentMoves) {
                                    if (m.Value.FirstMove[..4].Equals(previousMove[..4])) {
                                        index = m.Key;
                                        break;
                                    }
                                }

                                _opponentArrow = GetArrowOpponent(index, opponentMoves);
                                await AddArrowPlayer();
                                opponentMoves.Clear();
                            }

                            opponentMoves[move.Index] = move;
                        }
                    }
                }
            }

            if (!playerDone) {
                var line = await outputReader[0].ReadLineAsync();
                if (line is not null) {
                    if (line.StartsWith("bestmove")) {
                        playerDone = true;
                    }
                    else {
                        _status?.Report(line);
                        if (line.IndexOf(" multipv ", StringComparison.Ordinal) >= 0) {
                            var move = GetMove(line, board);
                            if (_moves.Count > 0 && move.Depth > _moves.Values[0].Depth) {
                                ShowMoves();
                                _moves.Clear();
                            }

                            _moves[move.Index] = move;
                        }
                    }
                }
            }
        }

        foreach (var move in _moves.Values) {
            await inputWriter[0].WriteLineAsync($"position fen {currentFen} moves {move.FirstMove}");
            await inputWriter[0].FlushAsync();

            await inputWriter[0].WriteLineAsync("d");
            await inputWriter[0].FlushAsync();

            var newFen = string.Empty;
            while (await outputReader[0].ReadLineAsync() is { } rawline) {
                if (!rawline.StartsWith("Fen:")) {
                    continue;
                }

                newFen = rawline[5..].Trim();
                break;
            }

            var pos = newFen.IndexOf(" ", StringComparison.Ordinal);
            if (pos > 0) {
                newFen = newFen[..pos];
            }

            if (!string.IsNullOrEmpty(newFen)) {
                if (_openings.TryGetValue(newFen, out var opening)) {
                    move.Opening = opening;
                }
            }
        }

        ShowMoves();
        await AddArrowPlayer();

        for (var i = 0; i < 2; i++) {
            await inputWriter[i].WriteLineAsync("quit");
            await inputWriter[i].FlushAsync();

            await stockfish[i].WaitForExitAsync();
            stockfish[i].Close();
        }

        _status?.Report($"Done. The best move: {_moves[0].FirstPiece}{_moves[0].FirstMove}; score: {_moves[0].ScoreText} (depth: {_moves[0].Depth})");
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

        var zipBytes = await File.ReadAllBytesAsync(bookPath);
        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var entry = archive.GetEntry("openings.csv");
        if (entry != null) {
            await using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            var text = await reader.ReadToEndAsync();
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