using System.Windows.Media;

namespace Chezzz
{
    public class Move
    {
        public int Depth { get; set; }
        public string FirstMove { get; set; }
        public int Index { get; set; }
        public string Score { get; set; }
        public SolidColorBrush Color { get; set; }
        public string Forecast { get; set; }
        public string Moves { get; set; }

        public Move()
        {
            Depth = -1;
            FirstMove = string.Empty;
            Index = -1;
            Score = string.Empty;
            Color = Brushes.Black;
            Forecast = string.Empty;
            Moves = string.Empty;
        }
    }
}
