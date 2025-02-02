namespace Chezzz;

public class Move
{
    public int Index { get; set; }
    public int Depth { get; set; }
    public string FirstMove { get; set; }
    public string ScoreText { get; set; }
    public int ScoreValue { get; set; }
    public string Forecast { get; set; }
    public string Opening { get; set; }

    public Move()
    {
        Depth = -1;
        FirstMove = string.Empty;
        ScoreText = string.Empty;
        ScoreValue = 0;
        Forecast = string.Empty;
        Opening = string.Empty;
    }
}