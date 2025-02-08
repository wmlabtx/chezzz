namespace Chezzz;

public class Move
{
    public int Index { get; set; }
    public int Depth { get; set; }
    public string FirstMove { get; set; }
    public string ScoreText { get; set; }
    public int Score { get; set; }
    public string Forecast { get; set; }
    public string Opening { get; set; }

    public Move()
    {
        Index = -1;
        Depth = -1;
        FirstMove = string.Empty;
        ScoreText = string.Empty;
        Score = 0;
        Forecast = string.Empty;
        Opening = string.Empty;
    }
}