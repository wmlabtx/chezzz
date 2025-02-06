using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Chezzz;

public partial class MainWindow
{
    private static Color DarkenColor(Color color, double factor)
    {
        factor = Math.Max(0, Math.Min(1, factor));
        var r = (byte)(color.R * (1 - factor));
        var g = (byte)(color.G * (1 - factor));
        var b = (byte)(color.B * (1 - factor));
        return Color.FromRgb(r, g, b);
    }

    private static Color LigthenColor(Color color, double factor)
    {
        factor = Math.Max(0, Math.Min(1, factor));
        var r = (byte)((255 - color.R) * factor + color.R);
        var g = (byte)((255 - color.G) * factor + color.G);
        var b = (byte)((255 - color.B) * factor + color.B);
        return Color.FromRgb(r, g, b);
    }

    private static Color InterpolateColor(Color color1, Color color2, double factor)
    {
        factor = Math.Max(0, Math.Min(1, factor));
        var a = (byte)(color1.A + (color2.A - color1.A) * factor);
        var r = (byte)(color1.R + (color2.R - color1.R) * factor);
        var g = (byte)(color1.G + (color2.G - color1.G) * factor);
        var b = (byte)(color1.B + (color2.B - color1.B) * factor);
        return Color.FromArgb(a, r, g, b);
    }

    private static Color GetColor(int scoreValue, int minScoreNegative, int maxScorePositive)
    {
        switch (scoreValue) {
            case NEGATIVE_MATE:
                return Colors.DarkRed;
            case POSITIVE_MATE:
                return Colors.DarkGreen;
        }

        scoreValue = Math.Max(minScoreNegative, Math.Min(maxScorePositive, scoreValue));

        double normalizedValue;
        if (minScoreNegative == 0) {
            minScoreNegative = Math.Min(-1000, scoreValue);
        }

        if (scoreValue <= 0) {
            normalizedValue = (double)scoreValue / minScoreNegative;
            return InterpolateColor(Colors.Gray, Colors.Red, normalizedValue);
        }

        if (maxScorePositive == 0) {
            maxScorePositive = Math.Max(1000, scoreValue);
        }

        normalizedValue = (double)scoreValue / maxScorePositive;
        return InterpolateColor(Colors.Gray, Colors.Green, normalizedValue);
    }

    private void UpdateRequiredScore()
    {
        DescreaseScore.IsEnabled = _requiredScoreValue > NEGATIVE_MATE;
        IncreaseScore.IsEnabled = _requiredScoreValue < POSITIVE_MATE;

        var color = GetColor(_requiredScoreValue, -1000, 1000);
        var darkenColor = DarkenColor(color, 0.5);
        var ligthenColor = LigthenColor(color, 0.5);
        var gradient = new LinearGradientBrush {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops = {
                new GradientStop(ligthenColor, 0),
                new GradientStop(color, 0.4),
                new GradientStop(darkenColor, 0.8),
                new GradientStop(darkenColor, 1)
            }
        };

        var requiredScoreText = _requiredScoreValue switch {
            POSITIVE_MATE => "MAX",
            NEGATIVE_MATE => "MIN",
            _ => _requiredScoreValue < 0
                ? $"-{Math.Abs(_requiredScoreValue) / 100.0:F2}"
                : $"+{_requiredScoreValue / 100.0:F2}"
        };

        RequiredScoreText.Text = requiredScoreText;
        RequiredScore.Background = gradient;
        RequiredScore.BorderBrush = new SolidColorBrush(color);
    }

    private void UpdateRequiredTime()
    {
        var str = (_requiredTimeMs / 1000.0).ToString("0.#");
        Time.Content = $"{str}s";
    }

    private void ShowMoves()
    {
        UpdateRequiredScore();

        Panel.Children.Clear();

        var moves = _moves.Values.OrderByDescending(move => move.ScoreValue).ToArray();

        var minScoreNegative = NEGATIVE_MATE;
        var notmates = _moves.Values.Where(move => move.ScoreValue <= 0 && move.ScoreValue != NEGATIVE_MATE).ToArray();
        if (notmates.Length > 0) {
            minScoreNegative = notmates.Min(move => move.ScoreValue);
        }

        var maxScorePositive = POSITIVE_MATE;
        notmates = _moves.Values.Where(move => move.ScoreValue >= 0 && move.ScoreValue != POSITIVE_MATE).ToArray();
        if (notmates.Length > 0) {
            maxScorePositive = notmates.Max(move => move.ScoreValue);
        }

        foreach (var move in moves) {
            var color = GetColor(move.ScoreValue, minScoreNegative, maxScorePositive);
            var darkenColor = DarkenColor(color, 0.5);
            var ligthenColor = LigthenColor(color, 0.5);
            var gradient = new LinearGradientBrush {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = {
                    new GradientStop(ligthenColor, 0),
                    new GradientStop(color, 0.4),
                    new GradientStop(darkenColor, 0.8),
                    new GradientStop(darkenColor, 1)
                }
            };

            var grid = new Grid();
            if (string.IsNullOrEmpty(move.Opening)) {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            else {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            var scoreTextBlock = new TextBlock {
                Text = move.ScoreText,
                Foreground = Brushes.White,
                Padding = new Thickness(4, 0, 0, 0)
            };

            var wdlTextBlock = new TextBlock {
                Text = move.Forecast,
                Foreground = Brushes.White,
                Padding = new Thickness(4, 0, 4, 0)
            };

            var firstmoveTextBlock = new TextBlock {
                Text = move.FirstMove,
                Foreground = Brushes.White,
                Padding = new Thickness(4, 0, 4, 0)
            };

            var openingTextBlock = new TextBlock {
                Text = move.Opening,
                Foreground = Brushes.White,
                Padding = new Thickness(4, 0, 4, 0)
            };

            if (string.IsNullOrEmpty(move.Opening)) {
                scoreTextBlock.SetValue(Grid.ColumnProperty, 0);
                wdlTextBlock.SetValue(Grid.ColumnProperty, 1);
                firstmoveTextBlock.SetValue(Grid.ColumnProperty, 2);

                grid.Children.Add(scoreTextBlock);
                grid.Children.Add(wdlTextBlock);
                grid.Children.Add(firstmoveTextBlock);
            }
            else {
                firstmoveTextBlock.SetValue(Grid.ColumnProperty, 0);
                openingTextBlock.SetValue(Grid.ColumnProperty, 1);

                grid.Children.Add(firstmoveTextBlock);
                grid.Children.Add(openingTextBlock);
            }

            var moveLabel = new Label {
                Margin = new Thickness(0, 0, 2, 0),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(color),
                Content = grid,
                Background = gradient,
            };

            if (move.Index == _currentIndex) {
                Panel.Children.Add(moveLabel);
            }
            else {
                var moveButton = new Button {
                    Margin = new Thickness(0, 0, 2, 0),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(color),
                    Background = gradient,
                    Content = string.IsNullOrEmpty(move.Opening) ? "" : "B",
                    MinWidth = 8,
                    Foreground = Brushes.White,
                    Cursor = Cursors.Hand,
                    Tag = move,
                    ToolTip = moveLabel
                };

                moveButton.Click += (sender, e) => {
                    var buttonSender = (Button)sender;
                    var moveSender = (Move)buttonSender.Tag;
                    _currentIndex = moveSender.Index;
                    ShowMoves();
                };

                ToolTipService.SetInitialShowDelay(moveButton, 0);
                Panel.Children.Add(moveButton);
            }
        }
    }
}

