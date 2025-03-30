using System.Text.RegularExpressions;

namespace Chezzz.San;

public class Board
{
    private static readonly Regex RegexSanOneMove = new(@"(^([PNBRQK])?([a-h])?([1-8])?(x|X|-)?([a-h][1-8])(=[NBRQ]| ?e\.p\.)?|^O-O(-O)?)(\+|\#|\$)?$", RegexOptions.Compiled);

    private readonly Piece[,] _pieces = new Piece[8, 8];
    private bool _isGame;
    private readonly List<Move> _movelist = new();

    public Board()
    {
        for (var y = 0; y < 8; y++) {
            for (var x = 0; x < 8; x++) {
                _pieces[y, x] = new Piece();
            }
        }
    }

    public void PutPiece(int y, int x, Piece piece)
    {
        _pieces[y, x] = piece;
    }

    public void PutPiece(Position position, Piece piece)
    {
        PutPiece(position.Y, position.X, piece);
    }

    private void ClearPosition(int y, int x)
    {
        var pieceOnPosition = _pieces[y, x];
        if (pieceOnPosition == null) {
            throw new Exception();
        }

        _pieces[y, x] = new Piece();
    }

    private void ClearPosition(Position position)
    {
        ClearPosition(position.Y, position.X);
    }

    public Piece? GetPiece(int y, int x)
    {
        var piece = _pieces[y, x];
        return piece.Type == '.' ? null : piece;
    }

    public Piece? GetPiece(Position position)
    {
        return GetPiece(position.Y, position.X);
    }

    public void StartGame()
    {
        _isGame = true;

        PutPiece(new Position("a1"), new Piece('w', 'r'));
        PutPiece(new Position("b1"), new Piece('w', 'n'));
        PutPiece(new Position("c1"), new Piece('w', 'b'));
        PutPiece(new Position("d1"), new Piece('w', 'q'));
        PutPiece(new Position("e1"), new Piece('w', 'k'));
        PutPiece(new Position("f1"), new Piece('w', 'b'));
        PutPiece(new Position("g1"), new Piece('w', 'n'));
        PutPiece(new Position("h1"), new Piece('w', 'r'));

        PutPiece(new Position("a2"), new Piece('w', 'p'));
        PutPiece(new Position("b2"), new Piece('w', 'p'));
        PutPiece(new Position("c2"), new Piece('w', 'p'));
        PutPiece(new Position("d2"), new Piece('w', 'p'));
        PutPiece(new Position("e2"), new Piece('w', 'p'));
        PutPiece(new Position("f2"), new Piece('w', 'p'));
        PutPiece(new Position("g2"), new Piece('w', 'p'));
        PutPiece(new Position("h2"), new Piece('w', 'p'));

        PutPiece(new Position("a7"), new Piece('b', 'p'));
        PutPiece(new Position("b7"), new Piece('b', 'p'));
        PutPiece(new Position("c7"), new Piece('b', 'p'));
        PutPiece(new Position("d7"), new Piece('b', 'p'));
        PutPiece(new Position("e7"), new Piece('b', 'p'));
        PutPiece(new Position("f7"), new Piece('b', 'p'));
        PutPiece(new Position("g7"), new Piece('b', 'p'));
        PutPiece(new Position("h7"), new Piece('b', 'p'));

        PutPiece(new Position("a8"), new Piece('b', 'r'));
        PutPiece(new Position("b8"), new Piece('b', 'n'));
        PutPiece(new Position("c8"), new Piece('b', 'b'));
        PutPiece(new Position("d8"), new Piece('b', 'q'));
        PutPiece(new Position("e8"), new Piece('b', 'k'));
        PutPiece(new Position("f8"), new Piece('b', 'b'));
        PutPiece(new Position("g8"), new Piece('b', 'n'));
        PutPiece(new Position("h8"), new Piece('b', 'r'));
    }

    public bool Move(string san)
    {
        if (!_isGame) {
            throw new Exception();
        }

        var turnColor = _movelist.Count % 2 == 0 ? 'w' : 'b';

        var matches = RegexSanOneMove.Matches(san);
        if (matches.Count == 0) {
            return false;
        }

        var move = new Move();
        var isCapture = false;

        foreach (var group in matches[0].Groups.Values) {
            if (!group.Success) {
                continue;
            }

            var value = group.Value;
            string parameter;
            switch (group.Name) {
                case "1":
                    if (value.Equals("O-O") || value.Equals("O-O-O")) {
                        parameter = value.ToLower().Trim();
                        move.Parameter = parameter;
                        move.CastleType = parameter switch {
                            "o-o" => 'k',
                            "o-o-o" => 'q',
                            _ => throw new Exception()
                        };

                        if (turnColor == 'w') {
                            move.From = new Position("e1");
                            move.To = value switch {
                                "O-O" => new Position("g1"),
                                "O-O-O" => new Position("c1"),
                                _ => move.To
                            };
                        }
                        else {
                            move.From = new Position("e8");
                            move.To = value switch {
                                "O-O" => new Position("g8"),
                                "O-O-O" => new Position("c8"),
                                _ => move.To
                            };
                        }
                        
                        move.Piece = GetPiece(move.From) ?? new Piece(turnColor, 'k');
                    }

                    break;
                case "2":
                    move.Piece = new Piece(turnColor, char.ToLower(value[0]));
                    break;
                case "3":
                    move.From.X = Position.FromFile(value[0]);
                    break;
                case "4":
                    move.From.Y = Position.FromRank(value[0]);
                    break;
                case "5":
                    if (value.Equals("x") || value.Equals("X")) {
                        isCapture = true;
                    }

                    break;
                case "6":
                    move.To = new Position(value);
                    break;
                case "7":
                    parameter = value.ToLower().Trim();
                    move.Parameter = parameter;
                    switch (parameter) {
                        case "o-o":
                            move.CastleType = 'k';
                            break;
                        case "o-o-o":
                            move.CastleType = 'q';
                            break;
                        case "=":
                            move.PromotionPieceType = 'q';
                            break;
                        case "=q":
                            move.PromotionPieceType = 'q';
                            break;
                        case "=r":
                            move.PromotionPieceType = 'r';
                            break;
                        case "=b":
                            move.PromotionPieceType = 'b';
                            break;
                        case "=n":
                            move.PromotionPieceType = 'n';
                            break;
                    }
                    break;
            }
        }

        move.Piece ??= new Piece(turnColor, 'p');
        if (isCapture) {
            var capturedPiece = GetPiece(move.To);
            if (capturedPiece != null) {
                move.CapturedPiece = capturedPiece;
            }
        }

        if (!move.From.HasValue) {
            var ambiguousMoves = GetMovesOfPieceOnPosition(move.Piece, move.To).ToList();
            if (move.From.HasValueX) {
                ambiguousMoves.RemoveAll(m => m.From.X != move.From.X);
            }

            if (move.From.HasValueY) {
                ambiguousMoves.RemoveAll(m => m.From.Y != move.From.Y);
            }

            if (ambiguousMoves.Count != 1) {
                return false;
            }

            move.From = ambiguousMoves[0].From;

            if (ambiguousMoves[0].Parameter is "e.p.") {
                move.Parameter = "e.p.";
                move.CapturedPiece = ambiguousMoves[0].CapturedPiece;
            }
        }

        if (!IsValidMove(move)) {
            return false;
        }

        _movelist.Add(move);

        if (move.Parameter != null) {
            if (move.Parameter == "e.p.") {
                var piece = GetPiece(move.From);
                if (piece == null) {
                    throw new Exception();
                }

                PutPiece(move.To, move.Piece);
                ClearPosition(move.From);

                if (move.CapturedPawnPosition.HasValue) {
                    ClearPosition(move.CapturedPawnPosition);
                }
                else {
                    throw new Exception();
                }
            }
            else if (move.Parameter.Equals("o-o") || move.Parameter.Equals("o-o-o")) {
                var y = move.To.Y;
                switch (move.Parameter) {
                    case "o-o":
                        PutPiece(y, 6, new Piece(move.Piece.Color, 'k'));
                        PutPiece(y, 5, new Piece(move.Piece.Color, 'r'));
                        ClearPosition(y, 4);
                        ClearPosition(y, 7);
                        break;
                    case "o-o-o":
                        PutPiece(y, 2, new Piece(move.Piece.Color, 'k'));
                        PutPiece(y, 3, new Piece(move.Piece.Color, 'r'));
                        ClearPosition(y, 4);
                        ClearPosition(y, 0);
                        break;
                    default:
                        throw new Exception();
                }
            }
            else if (move.Parameter.StartsWith("=")) {
                if (move.PromotionPieceType == '.') {
                    throw new Exception();
                }

                PutPiece(move.To, new Piece(turnColor, move.PromotionPieceType));
                ClearPosition(move.From);
            }
            else {
                throw new Exception();
            }
            return true;
        }

        var pieceOnMove = GetPiece(move.From);
        if (pieceOnMove == null) {
            throw new Exception();
        }

        PutPiece(move.To, pieceOnMove);
        ClearPosition(move.From);

        return true;
    }

    public string GetLastMove()
    {
        if (_movelist.Count == 0) {
            return string.Empty;
        }

        var sanmove = _movelist.Last();
        var move = $"{sanmove.From}{sanmove.To}";
        return move;
    }

    public string ToFen()
    {
        if (!_isGame) {
            throw new Exception();
        }

        var isTurnWhite = _movelist.Count % 2 == 0;
        return ToFen(isTurnWhite);
    }

    public string ToFen(bool isTurnWhite)
    {
        // pieces

        var board = new char[8, 8];
        for (var y = 0; y < 8; y++) {
            for (var x = 0; x < 8; x++) {
                var piece = GetPiece(y, x);
                board[y, x] = piece?.ToFen() ?? '.';
            }
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

        var pieces = string.Join("/", fenParts);

        // color

        var color = isTurnWhite ? "w" : "b";

        // castling

        var castleWK = HasRightToCastle('w', 'k');
        var castleWQ = HasRightToCastle('w', 'q');
        var castleBK = HasRightToCastle('b', 'k');
        var castleBQ = HasRightToCastle('b', 'q');
        var castling = string.Empty;
        if (castleWK) {
            castling += "K";
        }

        if (castleWQ) {
            castling += "Q";
        }

        if (castleBK) {
            castling += "k";
        }

        if (castleBQ) {
            castling += "q";
        }

        if (string.IsNullOrEmpty(castling)) {
            castling = "-";
        }

        // passant

        var passant = "-";
        if (_isGame) {
            var epPosition = LastMoveEnPassantPosition();
            if (epPosition.HasValue) {
                passant = epPosition.ToString();
            }
        }

        // halfmoves

        var halfmoves = 0;
        if (_isGame) {
            halfmoves = GetHalfMovesCount();
        }

        // fullmoves

        var fullmoves = 1;
        if (_isGame) {
            fullmoves = _movelist.Count / 2 + 1;
        }

        var fen = string.Join(' ', pieces, color, castling, passant, halfmoves, fullmoves);
        return fen;
    }

    private bool HasRightToCastle(char side, char castleType)
    {
        var kingpos = new Position(side == 'w' ?  "e1":"e8");
        var king = GetPiece(kingpos);
        if (king == null) {
            return false;
        }

        if (king.Type != 'k' || king.Color != side) {
            return false;
        }

        var rookpos = castleType switch {
            'k' => new Position(side == 'w' ? "h1" : "h8"),
            'q' => new Position(side == 'w' ? "a1" : "a8"),
            _ => throw new Exception()
        };

        var rook = GetPiece(rookpos);
        if (rook == null) {
            return false;
        }

        if (rook.Type != 'r' || rook.Color != side) {
            return false;
        }

        if (_isGame) {
            return !PieceEverMoved(kingpos) && !PieceEverMoved(rookpos);
        }

        return true;
    }

    private bool PieceEverMoved(Position position)
    {
        return _movelist.Any(move => move.From.Equals(position));
    }

    private Position LastMoveEnPassantPosition()
    {
        var position = new Position();
        if (_movelist.Count == 0) {
            return position;
        }

        var lastMove = _movelist.Last();
        if (lastMove.Piece == null) {
            return position;
        }

        var isPawn = lastMove.Piece.Type == 'p';
        if (!isPawn) {
            return position;
        }

        var moving2Tiles = Math.Abs(lastMove.To.Y - lastMove.From.Y) == 2;
        if (!moving2Tiles) {
            return position;
        }

        position = new Position((lastMove.To.Y + lastMove.From.Y) / 2, lastMove.To.X);
        return position;
    }

    private int GetHalfMovesCount()
    {
        var index = _movelist.Count - 1;
        var moveFound = false;
        while (index >= 0 && !moveFound) {
            var move = _movelist.ElementAt(index);
            if (move.CapturedPiece != null || move.Piece is { Type: 'p' }) {
                moveFound = true;
            }

            index--;
        }

        if (moveFound) {
            index++;
        }

        var moveIndex = _movelist.Count - 1;
        return index >= 0 ? moveIndex - index : moveIndex + 1;
    }

    private IEnumerable<Move> GetMovesOfPieceOnPosition(Piece piece, Position to)
    {
        for (var y = 0; y < 8; y++) {
            for (var x = 0; x < 8; x++) {
                var pieceFrom = GetPiece(y, x);
                if (pieceFrom == null || 
                    pieceFrom.Color != piece.Color || pieceFrom.Type != piece.Type ||
                    (to.Y == y && to.X == x)) {
                    continue;
                }

                var from = new Position(y, x);
                var ambiguousMove = new Move {
                    Piece = piece,
                    From = from,
                    To = to
                };

                if (IsValidMove(ambiguousMove)) {
                    yield return ambiguousMove;
                }
            }
        }
    }

    private bool IsValidMove(Move move)
    {
        if (move.Piece == null) {
            return false;
        }

        return move.Piece.Type switch {
            'p' => PawnValidation(move),
            'r' => RookValidation(move),
            'n' => KnightValidation(move),
            'b' => BishopValidation(move),
            'q' => QueenValidation(move),
            'k' => KingValidation(move),
            _ => false
        };
    }

    private bool PawnValidation(Move move)
    {
        if (move.Piece == null) {
            return false;
        }

        var isValid = false;
        var verticalDifference = move.To.Y - move.From.Y;
        var horizontalDifference = move.To.X - move.From.X;
        var verticalStep = Math.Abs(verticalDifference);
        var horizontalStep = Math.Abs(horizontalDifference);
        var pieceColor = move.Piece.Color;
        if ((pieceColor == 'w' && verticalDifference < 0) || (pieceColor == 'b' && verticalDifference > 0)) {
            if (horizontalStep == 0 && verticalStep == 1 && GetPiece(move.To) == null) {
                if (move.To.Y is 0 or 7) {
                    move.Parameter = "=";
                }
                
                isValid = true;
            }
            else if (horizontalStep == 0 && verticalStep == 2 && 
                     ((move.From.Y == 1 && GetPiece(2, move.To.X) == null && GetPiece(3, move.To.X) == null) ||
                      (move.From.Y == 6 && GetPiece(5, move.To.X) == null && GetPiece(4, move.To.X) == null))) {
                isValid = true;
            }
            else if (verticalStep == 1 && horizontalStep == 1 && 
                     GetPiece(move.To) != null && 
                     pieceColor != GetPiece(move.To)!.Color) {
                if (move.To.Y is 0 or 7) {
                    move.Parameter = "=";
                }

                isValid = true;
            }
            else if (IsValidEnPassant(move, verticalDifference, horizontalDifference)) {
                move.Parameter = "e.p.";
                move.CapturedPawnPosition = new Position(move.To.Y - verticalDifference, move.To.X);
                move.CapturedPiece = new Piece(pieceColor == 'w'? 'b' : 'w', 'p');
                isValid = true;
            }
        }

        return isValid;
    }

    private bool IsValidEnPassant(Move move, int v, int h)
    {
        if (move.Piece == null) {
            return false;
        }

        if (Math.Abs(v) == 1 && Math.Abs(h) == 1) {
            var piece = GetPiece(new Position(move.To.Y - v, move.To.X));
            if (piece != null && piece.Color != move.Piece.Color && piece.Type == 'p') {
                var lastMove = LastMoveEnPassantPosition();
                return lastMove.X == move.To.X && lastMove.Y == move.To.Y;
            }
        }

        return false;
    }

    private bool RookValidation(Move move)
    {
        if (move.Piece == null) {
            return false;
        }

        var verticalDiff = move.To.Y - move.From.Y;
        var horizontalDiff = move.To.X - move.From.X;

         if (verticalDiff != 0 && horizontalDiff != 0) {
             return false;
         }

        var stepVertical = Math.Sign(verticalDiff);
        var stepHorizontal = Math.Sign(horizontalDiff);

        var y = move.From.Y + stepVertical;
        var x = move.From.X + stepHorizontal;

        while (y != move.To.Y || x != move.To.X) {
            if (GetPiece(y, x) != null) {
                return false;
            }

            y += stepVertical;
            x += stepHorizontal;
        }

        return GetPiece(y, x)?.Color != move.Piece.Color;
    }

    private bool KnightValidation(Move move)
    {
        if (move.Piece == null) {
            return false;
        }

        var verticalDiff = Math.Abs(move.To.X - move.From.X);
        var horizontalDiff = Math.Abs(move.To.Y - move.From.Y);
        if ((verticalDiff == 2 && horizontalDiff == 1) || (verticalDiff == 1 && horizontalDiff == 2)) {
            return GetPiece(move.To)?.Color != move.Piece.Color;
        }

        return false;
    }

    private bool BishopValidation(Move move)
    {
        if (move.Piece == null) {
            return false;
        }

        var verticalDiff = move.To.Y - move.From.Y;
        var horizontalDiff = move.To.X - move.From.X;
        if (Math.Abs(verticalDiff) != Math.Abs(horizontalDiff)) {
            return false;
        }

        var stepVertical = Math.Sign(verticalDiff);
        var stepHorizontal = Math.Sign(horizontalDiff);

        var y = move.From.Y + stepVertical;
        var x = move.From.X + stepHorizontal;

        while (y != move.To.Y && x != move.To.X) {
            if (GetPiece(y, x) != null) {
                return false;
            }

            y += stepVertical;
            x += stepHorizontal;
        }

        return GetPiece(y, x)?.Color != move.Piece.Color;
    }

    private bool KingValidation(Move move)
    {
        if (move.Piece == null) {
            return false;
        }

        if (Math.Abs(move.To.X - move.From.X) < 2 && Math.Abs(move.To.Y - move.From.Y) < 2) {
            return GetPiece(move.To)?.Color != move.Piece.Color;
        }

        var kingMovesHorizontally = move.From.Y == move.To.Y;
        var kingOnBeginPos = move.From.X == 4 && move.To.Y is 0 or 7;

        if (!kingOnBeginPos || !kingMovesHorizontally) {
            return false;
        }

        var kingMoves2Tiles = Math.Abs(move.To.X - move.From.X) == 2;
        var kingMovesOnRook = move.To.X is 0 or 7;

        if (!kingMovesOnRook && !kingMoves2Tiles) {
            return false;
        }

        var x = kingMovesOnRook ? (move.To.X == 0 ? 2 : 6) : move.To.X;

        var isKingSideCastle = x == 6;
        var isQueenSideCastle = x == 2;

        move.CastleType = isKingSideCastle ? 'k' : 'q';

        var y = move.To.Y;
        var hasObstacles = true;
        if (isQueenSideCastle) {
            hasObstacles = GetPiece(y, 1) != null || GetPiece(y, 2) != null || GetPiece(y, 3) != null;
        }
        else if (isKingSideCastle) {
            hasObstacles = GetPiece(y, 5) != null || GetPiece(y, 6) != null;
        }

        var isValid = !hasObstacles && HasRightToCastle(move.Piece.Color, move.CastleType);
        if (isValid && kingMovesOnRook) {
            move.To = new Position(move.To.Y, move.To.X == 0 ? 2 : 6);
        }

        return isValid;
    }

    private bool QueenValidation(Move move)
    {
        return BishopValidation(move) || RookValidation(move);
    }
}