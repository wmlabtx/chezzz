using System.Text.RegularExpressions;

namespace Chezzz;

public partial class MainWindow
{
    [GeneratedRegex(@"<wc-chess-board[^>]*\bclass\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex ChessBoardRegex();

    [GeneratedRegex(@"<div\s+class=""cg-wrap\s+orientation-(?<orientation>\w+)\s+manipulable""><cg-container\s+style=""width:\s*(?<width>\d+)px;\s*height:\s*(?<height>\d+)px;"">", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex OrientationRegex();

    [GeneratedRegex(@"<kwdb[^>]*>(.*?)<\/kwdb>")]
    private static partial Regex KwDbRegex();
    
    [GeneratedRegex(@"<span class=""node-highlight-content offset-for-annotation-icon(?:\s+selected)?"">(?:<span.*?data-figurine=""([^""]+)""></span>)?\s*([^<]+)<")]
    private static partial Regex DataFigurineRegex();

    private static bool ProcessSanMoves(List<string> sanmoves, out Chess.ChessBoard sanBoard, out string previousFen, out string previousMove, out string currentFen)
    {
        previousFen = string.Empty;
        previousMove = string.Empty;
        currentFen = string.Empty;
        sanBoard = new Chess.ChessBoard();

        for (var i = 0; i < sanmoves.Count; i++) {
            if (!sanBoard.Move(sanmoves[i])) {
                return false;
            }

            if (i == sanmoves.Count - 2) {
                previousFen = sanBoard.ToFen();
            }

            if (i == sanmoves.Count - 1) {
                currentFen = sanBoard.ToFen();
                var executedMoves = sanBoard.ExecutedMoves.ToArray();
                var lastMove = executedMoves[^1];
                previousMove = $"{lastMove.OriginalPosition}{lastMove.NewPosition}";
            }
        }

        return true;
    }

    private void GetFenFromChess(string decodedHtml, out string error, out Chess.ChessBoard board, out string previousFen, out string previousMove, out string currentFen)
    {
        board = new Chess.ChessBoard();
        error = string.Empty;
        previousFen = string.Empty;
        previousMove = string.Empty;
        currentFen = board.ToFen();
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

        var sanmoves = new List<string>();

        // <span class="node-highlight-content offset-for-annotation-icon">d4 </span></div>
        // <span class="node-highlight-content offset-for-annotation-icon"><span class="icon-font-chess rook-white " data-figurine="R"></span> xf6 </span></div>
        // <span class="node-highlight-content offset-for-annotation-icon selected">b1=Q+ </span></div>
        // <span class="node-highlight-content offset-for-annotation-icon">hxg6 <div class="move-info-icon" data-tooltip="En passant is a special pawn move by which a pawn captures another pawn that has advanced two squares." style="--tooltip-top:1px"><span class="icon-font-chess circle-info"></span></div>
        // "Qd1+<draw title=\"Draw offer\">½?</draw>"

        var matches = DataFigurineRegex().Matches(decodedHtml);
        foreach (var m in matches.Cast<Match>()) {
            var isSelected = m.Value.Contains("selected");
            var moveText = m.Groups[2].Value.Trim();
            var figurine = m.Groups[1].Success ? m.Groups[1].Value : "";
            var completeMove = !string.IsNullOrEmpty(figurine) ? figurine + moveText : moveText;
            int index = completeMove.IndexOf('<');
            if (index > 0) {
                completeMove = completeMove[..index].Trim();
            }

            sanmoves.Add(completeMove);
            if (isSelected) {
                break;
            }
        }

        if (sanmoves.Count > 0) {
            if (!ProcessSanMoves(sanmoves, out board, out previousFen, out previousMove, out currentFen)) {
                error = "Error processing moves";
            }
        }
    }

    private void GetFenFromLiChess(string decodedHtml, out string error, out Chess.ChessBoard board, out string previousFen, out string previousMove, out string currentFen)
    {
        board = new Chess.ChessBoard();
        error = string.Empty;
        previousFen = string.Empty;
        previousMove = string.Empty;
        currentFen = string.Empty;

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
        if (orientation.Equals("black", StringComparison.Ordinal)) {
            _isWhite = false;
        }
        else {
            if (!orientation.Equals("white", StringComparison.Ordinal)) {
                error = $"Unknown orientation '{orientation}'";
                return;
            }
        }

        var sanmoves = new List<string>();
        var simpleMoveListMatch = KwDbRegex().Matches(decodedHtml);
        foreach (Match match in simpleMoveListMatch) {
            if (match.Groups.Count > 1) {
                var move = match.Groups[1].Value;
                sanmoves.Add(move);
            }
        }

        if (sanmoves.Count > 0) {
            if (!ProcessSanMoves(sanmoves, out board, out previousFen, out previousMove, out currentFen)) {
                error = "Error processing moves";
            }
        }
    }

    private static string GetPiece(string move, Chess.ChessBoard board)
    {
        var piece = board[move[..2]];
        if (piece == null) {
            return string.Empty;
        }

        var f = piece.ToFenChar();
        var pieceUnicodeChar = f switch {
            'P' => "",
            'N' => "\u2658",
            'B' => "\u2657",
            'R' => "\u2656",
            'Q' => "\u2655",
            'K' => "\u2654",
            'p' => "",
            'n' => "\u265E",
            'b' => "\u265D",
            'r' => "\u265C",
            'q' => "\u265B",
            'k' => "\u265A",
            _ => f.ToString()
            };

        return pieceUnicodeChar;
    }
}
