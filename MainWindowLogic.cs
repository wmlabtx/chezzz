using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Chezzz;

public partial class MainWindow
{
    private readonly IProgress<string>? _status;
    private readonly List<Border> _scoreBorders = new();
    private readonly List<Border> _wdlBorders = new();
    private readonly List<Border> _bestMoveBorders = new();
    private readonly List<Label> _scoreTexts = new();
    private readonly List<Label> _wdlTexts = new();
    private readonly List<Label> _bestMoveTexts = new();

    private const int MoveMaxCount = 3 ;

    private string? _stockfishPath;

    private void WindowLoaded()
    {
        const int margin = 10;

        Left = SystemParameters.WorkArea.Left + margin;
        Top = SystemParameters.WorkArea.Top + margin;
        Width = SystemParameters.WorkArea.Width - margin * 2;
        Height = SystemParameters.WorkArea.Height - margin * 2;
        Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - margin - Width) / 2;
        Top = SystemParameters.WorkArea.Top + (SystemParameters.WorkArea.Height - margin - Height) / 2;

        var index = 0;
        for (var col = 0; col < 3 * MoveMaxCount; col += 3) {
            var scoreBorder = new Border {
                Name = $"ScoreBorder{index}",
                Background = Brushes.Green,
                Margin = new Thickness(10, 4, 0, 4),
                CornerRadius = new CornerRadius(10, 0, 0, 10),
                Visibility = Visibility.Hidden
            };
            scoreBorder.SetValue(Grid.ColumnProperty, col);
            scoreBorder.SetValue(Grid.RowProperty, 0);
            MovesGrid.Children.Add(scoreBorder);
            _scoreBorders.Add(scoreBorder);

            var scoreText = new Label {
                Name = $"ScoreText{index}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4),
                Foreground = Brushes.White,
                /*FontWeight = FontWeights.Bold,*/
                Content = "+0.22"
            };
            scoreBorder.Child = scoreText;
            _scoreTexts.Add(scoreText);

            var wdlBorder = new Border {
                Name = $"WdlBorder{index}",
                Background = Brushes.Green,
                Margin = new Thickness(2, 4, 0, 4),
                Visibility = Visibility.Hidden
            };
            wdlBorder.SetValue(Grid.ColumnProperty, col + 1);
            wdlBorder.SetValue(Grid.RowProperty, 0);
            MovesGrid.Children.Add(wdlBorder);
            _wdlBorders.Add(wdlBorder);

            var wdlText = new Label {
                Name = $"WdlText{index}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4),
                Foreground = Brushes.White,
                /*FontWeight = FontWeights.Bold,*/
                Content = "win 100%"
            };
            wdlBorder.Child = wdlText;
            _wdlTexts.Add(wdlText);

            // add contour for best move border

            var bestMoveBorder = new Border {
                Name = $"BestMoveBorder{index}",
                Background = Brushes.DimGray,
                Margin = new Thickness(2, 4, 0, 4),
                CornerRadius = new CornerRadius(0, 10, 10, 0),
                BorderBrush = Brushes.Green,
                BorderThickness = new Thickness(2),
                Visibility = Visibility.Hidden
            };
            bestMoveBorder.SetValue(Grid.ColumnProperty, col + 2);
            bestMoveBorder.SetValue(Grid.RowProperty, 0);
            MovesGrid.Children.Add(bestMoveBorder);
            _bestMoveBorders.Add(bestMoveBorder);

            var bestMoveText = new Label {
                Name = $"BestMoveText{index}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4),
                Foreground = Brushes.White,
                /*FontWeight = FontWeights.Bold,*/
                Content = "c1g5"
            };
            bestMoveBorder.Child = bestMoveText;
            _bestMoveTexts.Add(bestMoveText);

            index++;
        }

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

    private void GetFenFromChess(string decodedHtml, out string error, out char[,] board, out string fen)
    {
        error = string.Empty;
        board = new char[8, 8];
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

    private static void GetFenFromLiChess(string decodedHtml, out string error, out char[,] board, out string fen)
    {
        error = string.Empty;
        board = new char[8, 8];
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
        var board = new char[8, 8];
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

        await inputWriter.WriteLineAsync("setoption name MultiPV value 32");
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

        await inputWriter.WriteLineAsync("go depth 16");
        await inputWriter.FlushAsync();

        var moves = new SortedList<int, Move>();
        while ((line = await outputReader.ReadLineAsync()) != null) {
            
            if (line.StartsWith("bestmove")) {
                _status?.Report("Done");
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
                    move.Score = parts[i + 1].StartsWith('-') ? $"-M{parts[i + 1][1..]}" : $"+M{parts[i + 1]}";
                    continue;
                }

                if (parts[i].Equals("cp")) {
                    move.ScoreI = int.Parse(parts[i + 1]);
                    move.Score = move.ScoreI < 0 ? $"-{Math.Abs(move.ScoreI) / 100.0:F2}" : $"+{move.ScoreI / 100.0:F2}";
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
                    move.ScoreColor = iwdl switch {
                        0 => Brushes.Green,
                        1 => Brushes.Gray,
                        2 => Brushes.Red,
                        _ => Brushes.Black
                    };

                    move.MoveColor = iwdl switch {
                        0 => Brushes.DarkGreen,
                        1 => Brushes.DimGray,
                        2 => Brushes.DarkRed,
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
                    var col = move.FirstMove[0] - 'a';
                    var row = '8' - move.FirstMove[1];
                    var f = char.ToUpper(board[row, col]).ToString();
                    var u = f switch {
                        "P" => "",
                        "N" => "\u265E ",
                        "B" => "\u265D ",
                        "R" => "\u265C ",
                        "Q" => "\u265B ",
                        "K" => "\u265A ",
                        _ => f,
                    };

                    move.FirstMove =  u + move.FirstMove;
                }
            }

            if (moves.Count > 0 && move.Depth > moves.Values[0].Depth) {
                ShowMoves(moves);
                moves.Clear();
            }

            moves[move.Index] = move;
        }

        ShowMoves(moves);

        inputWriter.Close();
        stockfish.Close();
    }

    private void ShowMoves(SortedList<int, Move> allMoves)
    {
        var moves = allMoves.Values.ToList();
        int i;
        if (moves.Count > MoveMaxCount) {
            i = 0;
            while (i < moves.Count && moves[i].Forecast[0] != 'l') {
                i++;
            }

            if (i >= MoveMaxCount) {
                while (moves.Count > i) {
                    moves.RemoveAt(moves.Count - 1);
                }
            }

            while (moves.Count > MoveMaxCount) {
                var mindiff = int.MaxValue;
                var minindex = -1;
                for (i = moves.Count - 2; i >= 0; i--) {
                    var diff = moves[i].ScoreI - moves[i + 1].ScoreI;
                    if (diff < mindiff) {
                        mindiff = diff;
                        minindex = i + 1;
                        if (minindex == moves.Count - 1) {
                            minindex = i;
                        }

                        if (mindiff == 0) {
                            break;
                        }
                    }
                }

                moves.RemoveAt(minindex);
            }
        }

        for (i = 0; i < moves.Count; i++) {
            _scoreBorders[i].Background = moves[i].ScoreColor;
            _scoreTexts[i].Content = moves[i].Score;
            _scoreBorders[i].Visibility = Visibility.Visible;

            _wdlBorders[i].Background = moves[i].ScoreColor;
            _wdlTexts[i].Content = moves[i].Forecast;
            _wdlBorders[i].Visibility = Visibility.Visible;

            _bestMoveTexts[i].Content = moves[i].FirstMove;
            _bestMoveBorders[i].Background = moves[i].MoveColor;
            _bestMoveBorders[i].BorderBrush = moves[i].ScoreColor;
            _bestMoveBorders[i].Visibility = Visibility.Visible;
        }

        if (moves.Count < MoveMaxCount) {
            for (i = allMoves.Count; i < MoveMaxCount; i++) {
                _scoreBorders[i].Visibility = Visibility.Hidden;
                _wdlBorders[i].Visibility = Visibility.Hidden;
                _bestMoveBorders[i].Visibility = Visibility.Hidden;
            }
        }
    }
}