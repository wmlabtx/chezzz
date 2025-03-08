using System.Text.RegularExpressions;

namespace Chezzz;

public partial class MainWindow
{
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

    [GeneratedRegex(@"<wc-simple-move-list(?<block>[\s\S]*?)</wc-simple-move-list>", RegexOptions.IgnoreCase)]
    private static partial Regex SimpleMoveListRegex();

    private static bool ProcessSanMoves(IEnumerable<string> sanmoves, out string fen)
    {
        fen = string.Empty;
        var sanBoard = new San.Board();
        sanBoard.StartGame();
        if (sanmoves.Any(move => !sanBoard.Move(move))) {
            return false;
        }

        fen = sanBoard.ToFen();
        return true;
    }

    private void GetFenFromChess(string decodedHtml, out string error, out San.Board board, out string fen)
    {
        board = new San.Board();

        error = string.Empty;
        fen = string.Empty;
        _isWhite = true;
        var match = ChessBoardRegex().Match(decodedHtml);
        if (match.Success) {
            var classValue = match.Groups[1].Value;
            if (classValue.Contains("flipped")) {
                _isWhite = false;
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

            board.PutPiece(row, col, new San.Piece(color, piece));
        }

        fen = board.ToFen(_isWhite);

        var sanmoves = new List<string>();
        var simpleMoveListMatch = SimpleMoveListRegex().Match(decodedHtml);
        if (simpleMoveListMatch.Success) {
            var simpleMoveListContent = simpleMoveListMatch.Groups["block"].Value;
            
            const string pattern = @"<span\s+class=""node-highlight-content[^""]*"">\s*(?:<span[^>]*data-figurine=""(?<figurine>[^""]+)""[^>]*></span>\s*)?(?<move>[^\s<]+)";
            var smatches = Regex.Matches(simpleMoveListContent, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            foreach (Match smatch in smatches) {
                var figurine = smatch.Groups["figurine"].Value.Trim();
                var movePart = smatch.Groups["move"].Value.Trim();
                var fullMove = string.IsNullOrEmpty(figurine) ? movePart : figurine + movePart;
                sanmoves.Add(fullMove);
            }
        }

        if (sanmoves.Count > 0) {
            if (ProcessSanMoves(sanmoves, out var sanfen)) {
                fen = sanfen;
            }
        }
    }

    private void GetFenFromLiChess(string decodedHtml, out string error, out San.Board board, out string fen)
    {
        board = new San.Board();

        error = string.Empty;
        fen = string.Empty;

        // <div class="cg-wrap orientation-white manipulable"><cg-container style="width: 736px; height: 736px;">
        // <div class="cg-wrap orientation-black manipulable"><cg-container style="width: 736px; height: 736px;">

        _isWhite = true;
        var regex = OrientationRegex();
        var m = regex.Match(decodedHtml);
        if (!m.Success) {
            error = "cg-wrap orientation not found";
            return;
        }

        var orientation = m.Groups["orientation"].Value;
        if (orientation.Equals("black")) {
            _isWhite = false;
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
            if (!_isWhite) {
                col = 7 - col;
                row = 7 - row;
            }

            board.PutPiece(row, col, new San.Piece(color, piece));
        }

        fen = board.ToFen(_isWhite);

        var sanmoves = new List<string>();
        const string pattern = @"<kwdb[^>]*>(.*?)<\/kwdb>";
        var simpleMoveListMatch = Regex.Matches(decodedHtml, pattern);
        foreach (Match match in simpleMoveListMatch) {
            if (match.Groups.Count > 1) {
                var move = match.Groups[1].Value;
                sanmoves.Add(move);
            }
        }

        if (sanmoves.Count > 0) {
            if (ProcessSanMoves(sanmoves, out var sanfen)) {
                fen = sanfen;
            }
        }
    }

    private static string GetPiece(string move, bool white, San.Board board)
    {
        var col = move[0] - 'a';
        var row = '8' - move[1];
        var piece = board.GetPiece(row, col);
        if (piece == null) {
            return string.Empty;
        }

        var f = piece.Type.ToString().ToUpper();
        string pieceUnicodeChar;
        if (white) {
            pieceUnicodeChar = f switch {
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
            pieceUnicodeChar = f switch {
                "P" => "",
                "N" => "\u265E",
                "B" => "\u265D",
                "R" => "\u265C",
                "Q" => "\u265B",
                "K" => "\u265A",
                _ => f
            };
        }

        return pieceUnicodeChar;
    }
}
