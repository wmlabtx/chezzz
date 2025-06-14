namespace Chezzz.Models;

public class Move
{
    public int Index { get; set; }
    public int Depth { get; set; }
    public string FirstMove { get; set; } = string.Empty;
    public string SecondMove { get; set; } = string.Empty;
    public string FirstPiece { get; set; } = string.Empty;
    public string SecondPiece { get; set; } = string.Empty;
    public Score Score { get; set; } = new Score(0, false);
    public string Forecast { get; set; } = string.Empty;
    public string Opening { get; set; } = string.Empty;

    public string GetSourceSquare() => FirstMove.Length >= 2 ? FirstMove[..2] : string.Empty;
    public string GetDestinationSquare() => FirstMove.Length >= 4 ? FirstMove[2..4] : string.Empty;
    public string GetSourceAndDestinationSquares() => FirstMove.Length >= 4 ? FirstMove[..4] : string.Empty;

    public override string ToString()
    {
        return $"{Index}:{FirstPiece}{FirstMove} ({Score})";
    }
}