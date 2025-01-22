using System.Windows.Media;

namespace Chezzz
{
    public class Move
    {
        public int Depth { get; set; }
        public string FirstMove { get; set; }
        public int Index { get; set; }
        public string Score { get; set; }
        public int ScoreI { get; set; }
        public SolidColorBrush ScoreColor { get; set; }
        public SolidColorBrush MoveColor { get; set; }
        public string Forecast { get; set; }

        public Move()
        {
            Depth = -1;
            FirstMove = string.Empty;
            Index = -1;
            Score = string.Empty;
            ScoreI = 0;
            ScoreColor = Brushes.Black;
            MoveColor = Brushes.Black;
            Forecast = string.Empty;
        }
    }
}
