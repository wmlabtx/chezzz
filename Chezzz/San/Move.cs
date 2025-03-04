namespace Chezzz.San;

public class Move
{
    public Piece? Piece { get; set; }
    public Piece? CapturedPiece { get; set; }

    // "o-o", "o-o-o", "e.p.", "=", "=q", "=r", "=b", "=n"
    public string? Parameter { get; set; }
    // '.', 'k', 'q'
    public char CastleType { get; set; }
    public Position CapturedPawnPosition { get; set; }
    public char PromotionPieceType { get; set; }

    public Position From { get; set; }
    public Position To { get; set; }

    public Move()
    {
        Piece = null;
        CapturedPiece = null;
        Parameter = null;
        CastleType = '.';
        CapturedPawnPosition = new Position();
        PromotionPieceType = '.';
        From = new Position();
        To = new Position();
    }
}