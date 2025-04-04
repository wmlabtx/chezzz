﻿using System.Text.RegularExpressions;

namespace Chezzz;

public partial class MainWindow
{
    [GeneratedRegex(@"<wc-chess-board[^>]*\bclass\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex ChessBoardRegex();

    [GeneratedRegex(@"<div\s+class=""cg-wrap\s+orientation-(?<orientation>\w+)\s+manipulable""><cg-container\s+style=""width:\s*(?<width>\d+)px;\s*height:\s*(?<height>\d+)px;"">", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex OrientationRegex();

    private static bool ProcessSanMoves(IReadOnlyList<string> sanmoves, out Chess.ChessBoard sanBoard, out string previousFen, out string previousMove, out string currentFen)
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
        currentFen = string.Empty;
        _isWhite = true;
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

        const string pattern = @"<span class=""node-highlight-content offset-for-annotation-icon(?:\s+selected)?"">(?:<span.*?data-figurine=""([^""]+)""></span>)?\s*([^<]+)</span>";

        var matches = Regex.Matches(decodedHtml, pattern);
        foreach (var m in matches.Cast<Match>()) {
            var isSelected = m.Value.Contains("selected");
            var moveText = m.Groups[2].Value.Trim();
            var figurine = m.Groups[1].Success ? m.Groups[1].Value : "";
            var completeMove = !string.IsNullOrEmpty(figurine) ? figurine + moveText : moveText;
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
        if (orientation.Equals("black")) {
            _isWhite = false;
        }
        else {
            if (!orientation.Equals("white")) {
                error = $"Unknown orientation '{orientation}'";
                return;
            }
        }

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
