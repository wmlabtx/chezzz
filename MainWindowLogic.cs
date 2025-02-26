using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
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
            _status?.Report($"{_stockfishPath} not found");
        }

        if (!Advice.IsEnabled) {
            return;
        }

        Advice.IsEnabled = false;
        await AdviceAsync();
        Advice.IsEnabled = true;
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
        if (board[7, 7] == 'R' && board[7, 4] == 'K') {
            castling += "K";
        }

        if (board[7, 0] == 'R' && board[7, 4] == 'K') {
            castling += "Q";
        }

        if (board[0, 7] == 'r' && board[0, 4] == 'k') {
            castling += "k";
        }

        if (board[0, 0] == 'r' && board[0, 4] == 'k') {
            castling += "q";
        }

        if (string.IsNullOrEmpty(castling)) {
            castling = "-";
        }

        var fen = ranksFen + $" {turn} {castling} - 0 1";
        return fen;
    }

    private void GetFenFromChess(string decodedHtml, out string error, out char[,] board, out string fen)
    {
        error = string.Empty;
        board = new char[8, 8];
        fen = string.Empty;
        isWhite = true;
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

    private void GetFenFromLiChess(string decodedHtml, out string error, out char[,] board, out string fen)
    {
        error = string.Empty;
        board = new char[8, 8];
        fen = string.Empty;

        // <div class="cg-wrap orientation-white manipulable"><cg-container style="width: 736px; height: 736px;">
        // <div class="cg-wrap orientation-black manipulable"><cg-container style="width: 736px; height: 736px;">

        isWhite = true;
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

    private static string GetPiece(string move, bool white, char[,] board)
    {
        var col = move[0] - 'a';
        var row = '8' - move[1];
        var f = char.ToUpper(board[row, col]).ToString();
        string piece;
        if (white) {
            piece = f switch {
                "P" => "",
                "N" => "\u2658",
                "B" => "\u2657",
                "R" => "\u2656",
                "Q" => "\u2655",
                "K" => "\u2654",
                _ => f
            };
        }
        else {
            piece = f switch {
                "P" => "",
                "N" => "\u265E",
                "B" => "\u265D",
                "R" => "\u265C",
                "Q" => "\u265B",
                "K" => "\u265A",
                _ => f
            };
        }

        return piece;
    }

    private async Task AdviceAsync()
    {
        const string script = "document.documentElement.outerHTML";
        var result = await WebBrowser.CoreWebView2.ExecuteScriptAsync(script);
        var decodedHtml = Regex.Unescape(result.Trim('"'));

        var error = string.Empty;
        var fen = string.Empty;
        var board = new char[8, 8];
        isWhite = true;
        switch (Platform.SelectionBoxItem) {
            case AppConsts.CHESS:
                GetFenFromChess(decodedHtml, out error, out board, out fen);
                break;
            case AppConsts.LICHESS:
                GetFenFromLiChess(decodedHtml, out error, out board, out fen);
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

        await inputWriter.WriteLineAsync("setoption name UCI_LimitStrength value false");
        await inputWriter.FlushAsync();

        await inputWriter.WriteLineAsync("setoption name Threads value 16");
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

        var requiredTime = _requiredTime.GetValue();
        await inputWriter.WriteLineAsync($"go movetime {requiredTime}");
        await inputWriter.FlushAsync();

        _selectedIndex = -1;
        _moves.Clear();
        await RemoveArrow();
        while ((line = await outputReader.ReadLineAsync()) != null) {
            
            if (line.StartsWith("bestmove")) {
                break;
            }

            _status?.Report(line);
            if (line.IndexOf(" multipv ", StringComparison.Ordinal) < 0) {
                continue;
            }

            var move = new Move();
            var parts = line.Split(' ');
            for (var i = 0; i < parts.Length; i++) {
                if (parts[i].Equals("multipv")) {
                    move.Index = int.Parse(parts[i + 1]) - 1;
                    continue;
                }

                if (parts[i].Equals("mate")) {
                    move.Score = parts[i + 1].StartsWith('-') ? NEGATIVE_MATE : POSITIVE_MATE;
                    var mateScore = 20 - Math.Min(20, Math.Abs(int.Parse(parts[i + 1])));
                    if (move.Score > 0) {
                        move.Score += mateScore;
                    }
                    else {
                        move.Score -= mateScore; 
                    }

                    move.ScoreText = parts[i + 1].StartsWith('-') ? $"-M{parts[i + 1][1..]}" : $"+M{parts[i + 1]}";
                    continue;
                }

                if (parts[i].Equals("cp")) {
                    move.Score = int.Parse(parts[i + 1]);
                    move.ScoreText = move.Score < 0 ? $"-{Math.Abs(move.Score) / 100.0:F2}" : $"+{move.Score / 100.0:F2}";
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
                    move.FirstPiece = GetPiece(move.FirstMove, isWhite, board);
                    if (i + 2 < parts.Length) {
                        move.SecondMove = parts[i + 2];
                        move.SecondPiece = GetPiece(move.SecondMove, !isWhite, board);
                    }
                }
            }

            if (_moves.Count > 0 && move.Depth > _moves.Values[0].Depth) {
                SetSelectedIndex(isWhite ? 1 : -1);
                ShowMoves();
                await AddArrow();
                _moves.Clear();
            }

            _moves[move.Index] = move;
        }

        foreach (var mv in _moves) {
            await inputWriter.WriteLineAsync($"position fen {fen} moves {mv.Value.FirstMove}");
            await inputWriter.FlushAsync();

            await inputWriter.WriteLineAsync("d");
            await inputWriter.FlushAsync();

            var newFen = string.Empty;
            while ((line = await outputReader.ReadLineAsync()) != null) {
                if (!line.StartsWith("Fen:")) {
                    continue;
                }

                newFen = line[5..].Trim();
                break;
            }

            var pos = newFen.IndexOf(" ", StringComparison.Ordinal);
            if (pos > 0) {
                newFen = newFen[..pos];
            }

            if (!string.IsNullOrEmpty(newFen)) {
                if (_openings.TryGetValue(newFen, out var opening)) {
                    _moves[mv.Key].Opening = opening;
                }
            }
        }

        await inputWriter.WriteLineAsync("quit");
        await inputWriter.FlushAsync();

        await stockfish.WaitForExitAsync(); 

        inputWriter.Close();
        stockfish.Close();

        SetSelectedIndex(isWhite ? 1 : -1);
        ShowMoves();
        await AddArrow();

        _status?.Report($"Done. Recommended move: {_moves[_selectedIndex].FirstPiece}{_moves[_selectedIndex].FirstMove}; score: {_moves[_selectedIndex].ScoreText} (depth: {_moves[_selectedIndex].Depth})");
    }

    private void SetSelectedIndex(int diff)
    {
        _selectedMoves.Clear();
        switch (_moves.Count) {
            case 0:
                _selectedIndex = -1;
                return;
            case 1:
                _selectedMoves.Add(0);
                _selectedIndex = 0;
                return;
        }

        const int MaxDiff = 25;
        const int MaxMoves = 5;
        int? minScore = null;
        int? maxScore = null;
        var requiredScore = _requiredScore.GetValue();
        foreach (var move in _moves.Values) {
            if (move.Score <= requiredScore + MaxDiff && move.Score >= requiredScore - MaxDiff && _selectedMoves.Count < MaxMoves) {
                _selectedMoves.Add(move.Index);
            }

            if (move.Score >= requiredScore) {
                minScore = move.Score;
            }

            if (move.Score <= requiredScore && maxScore == null) {
                maxScore = move.Score;
            }
        }

        if (_selectedMoves.Count == 0) {
            if (requiredScore >= 0) {
                if (minScore != null) {
                    foreach (var move in _moves.Values) {
                        if (move.Score <= minScore + MaxDiff && move.Score >= minScore && _selectedMoves.Count < MaxMoves) {
                            _selectedMoves.Add(move.Index);
                        }
                    }
                }
                else {
                    foreach (var move in _moves.Values) {
                        if (move.Score <= maxScore && move.Score >= maxScore - MaxDiff && _selectedMoves.Count < MaxMoves) {
                            _selectedMoves.Add(move.Index);
                        }
                    }
                }
            }
            else {
                if (maxScore != null) {
                    foreach (var move in _moves.Values) {
                        if (move.Score <= maxScore && move.Score >= maxScore - MaxDiff && _selectedMoves.Count < MaxMoves) {
                            _selectedMoves.Add(move.Index);
                        }
                    }
                }
                else {
                    foreach (var move in _moves.Values) {
                        if (move.Score <= minScore + MaxDiff && move.Score >= minScore && _selectedMoves.Count < MaxMoves) {
                            _selectedMoves.Add(move.Index);
                        }
                    }
                }
            }
        }

        var forwardMoves = new List<int>();
        foreach (var i in _selectedMoves) {
            switch (diff) {
                case > 0: {
                        if (_moves[i].FirstMove[3] > _moves[i].FirstMove[1]) {
                            forwardMoves.Add(i);
                        }
                        break;
                    }
                case < 0: {
                        if (_moves[i].FirstMove[3] < _moves[i].FirstMove[1]) {
                            forwardMoves.Add(i);
                        }
                        break;
                    }
            }
        }

        int random;
        if (forwardMoves.Count > 0) {
            random = RandomNumberGenerator.GetInt32(0, forwardMoves.Count);
            _selectedIndex = forwardMoves[random];
        }
        else {
            random = RandomNumberGenerator.GetInt32(0, _selectedMoves.Count);
            _selectedIndex = _selectedMoves.ElementAt(random);
        }
    }

    private void ChangeRequiredTime(int delta)
    {
        _requiredTime.ChangeValue(delta);
        UpdateRequiredTime();
    }

    private async void ChangeRequiredScore(int delta)
    {
        _requiredScore.ChangeValue(delta);
        UpdateRequiredScore();
        SetSelectedIndex(isWhite ? 1 : -1);
        ShowMoves();
        await AddArrow();
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

        _status?.Report("Reading named book moves...");

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

        _status?.Report("Done");
    }
}