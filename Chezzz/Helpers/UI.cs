namespace Chezzz.Helpers;

using System.Windows.Media;
using Color = System.Windows.Media.Color;

public static class UI
{
    public const string OPACITY = "0.7";

    public static Color InterpolateColor(Color color1, Color color2, double factor)
    {
        factor = Math.Max(0, Math.Min(1, factor));
        var a = (byte)(color1.A + (color2.A - color1.A) * factor);
        var r = (byte)(color1.R + (color2.R - color1.R) * factor);
        var g = (byte)(color1.G + (color2.G - color1.G) * factor);
        var b = (byte)(color1.B + (color2.B - color1.B) * factor);
        return Color.FromArgb(a, r, g, b);
    }

    public static Color DarkenColor(Color color, double factor)
    {
        factor = Math.Max(0, Math.Min(1, factor));
        var r = (byte)(color.R * (1 - factor));
        var g = (byte)(color.G * (1 - factor));
        var b = (byte)(color.B * (1 - factor));
        return Color.FromRgb(r, g, b);
    }

    public static Color LigthenColor(Color color, double factor)
    {
        factor = Math.Max(0, Math.Min(1, factor));
        var r = (byte)((255 - color.R) * factor + color.R);
        var g = (byte)((255 - color.G) * factor + color.G);
        var b = (byte)((255 - color.B) * factor + color.B);
        return Color.FromRgb(r, g, b);
    }

    public static Color GetColor(Models.Score score)
    {
        if (score.IsMate) {
            return score.Value >= 0 ? Colors.DarkGreen : Colors.DarkRed;
        }

        double normalizedValue;
        if (score.Value <= 0) {
            normalizedValue = (double)Math.Max(-500, score.Value) / -500;
            return InterpolateColor(Colors.Gray, Colors.Red, normalizedValue);
        }

        normalizedValue = (double)Math.Min(500, score.Value) / 500;
        return InterpolateColor(Colors.Gray, Colors.Green, normalizedValue);
    }
}
