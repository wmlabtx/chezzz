using Chezzz.Properties;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

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

    private static Color GetColor(int scoreValue)
    {
        if (scoreValue <= NEGATIVE_MATE) {
            return Colors.DarkRed;
        }

        if (scoreValue >= POSITIVE_MATE) {
            return Colors.DarkGreen;
        }

        double normalizedValue;
        if (scoreValue <= 0) {
            normalizedValue = (double)Math.Max(-500, scoreValue) / -500;
            return InterpolateColor(Colors.Gray, Colors.Red, normalizedValue);
        }

        normalizedValue = (double)Math.Min(500, scoreValue) / 500;
        return InterpolateColor(Colors.Gray, Colors.Green, normalizedValue);
    }

    private void UpdateRequiredTime()
    {
        DescreaseTime.IsEnabled = _requiredTime > _predefinedTime[0];
        IncreaseTime.IsEnabled = _requiredTime < _predefinedTime[^1];
        RequiredTimeText.Text = $"{_requiredTime / 1000.0:F1}s";
    }

    private void UpdateRequiredScore()
    {
        var requiredScoreText = _requiredScore switch {
            POSITIVE_MATE => "MAX",
            NEGATIVE_MATE => "MIN",
            _ => _requiredScore < 0
                ? $"-{Math.Abs(_requiredScore) / 100.0:F2}"
                : $"+{_requiredScore / 100.0:F2}"
        };

        DescreaseScore.IsEnabled = _requiredScore > NEGATIVE_MATE;
        IncreaseScore.IsEnabled = _requiredScore < POSITIVE_MATE;
        RequiredScoreText.Text = requiredScoreText;
    }

    private void ShowMoves()
    {
        Panel.Children.Clear();

        if (_selectedMoves.Length == 0) {
            _selectedMoves = _moves.Values.Take(10).ToArray();
        }

        var groups = _selectedMoves
            .OrderByDescending(move => move.Score)
            .GroupBy(move => move.FirstMove[..2])
            .Select(group => group.ToArray())
            .OrderByDescending(list => list.First().Score)
            .ToArray();

        foreach (var group in groups) {
            var bestMove = group.First();
            var groupLabel = new Label {
                Content = $"{bestMove.FirstPiece}{bestMove.FirstMove[..2]}",
                Foreground = Brushes.White,
                Margin = new Thickness(4, 0, 0, 0)
            };

            if (
                _selectedIndex >= 0 && 
                _selectedIndex < _moves.Count && 
                bestMove.FirstMove[..2].Equals(_moves[_selectedIndex].FirstMove[..2])) {
                groupLabel.Foreground = Brushes.Yellow;
            }

            Panel.Children.Add(groupLabel);
            foreach (var move in group) {
                var color = GetColor(move.Score);
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

                var grid = new Grid {
                    Margin = new Thickness(4, 0, 4, 0)
                };

                var column = 0;
                
                var scoreTextBlock = new TextBlock {
                    Text = move.ScoreText,
                    Foreground = Brushes.White,
                    Padding = new Thickness(0, 0, 0, 0)
                };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                scoreTextBlock.SetValue(Grid.ColumnProperty, column++);
                grid.Children.Add(scoreTextBlock);

                var wdlTextBlock = new TextBlock {
                    Text = move.Forecast,
                    Foreground = Brushes.White,
                    Padding = new Thickness(4, 0, 0, 0)
                };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                wdlTextBlock.SetValue(Grid.ColumnProperty, column++);
                grid.Children.Add(wdlTextBlock);

                var firstMoveTextBlock = new TextBlock {
                    Text = $"{move.FirstPiece}{move.FirstMove}",
                    Foreground = Brushes.White,
                    Padding = new Thickness(4, 0, 0, 0)
                };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                firstMoveTextBlock.SetValue(Grid.ColumnProperty, column++);
                grid.Children.Add(firstMoveTextBlock);

                if (!string.IsNullOrEmpty(move.Opening)) {
                    var openingTextBlock = new TextBlock {
                        Text = move.Opening,
                        Foreground = Brushes.White,
                        Padding = new Thickness(4, 0, 0, 0)
                    };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    openingTextBlock.SetValue(Grid.ColumnProperty, column);
                    grid.Children.Add(openingTextBlock);
                }
                else if (!string.IsNullOrEmpty(move.SecondMove)) {
                    var secondMoveTextBlock = new TextBlock {
                        Text = $"\u2026{move.SecondPiece}{move.SecondMove}?",
                        Foreground = Brushes.White,
                        Padding = new Thickness(4, 0, 0, 0)
                    };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    secondMoveTextBlock.SetValue(Grid.ColumnProperty, column);
                    grid.Children.Add(secondMoveTextBlock);
                }

                var tooltip = new ToolTip {
                    Content = grid,
                    Background = gradient
                };

                var moveButton = new Button {
                    Margin = new Thickness(1, 0, 0, 0),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(color),
                    Background = gradient,
                    Content = move.FirstMove.Substring(2, 2),
                    Foreground = Brushes.White,
                    Width = 21,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Cursor = Cursors.Hand,
                    Tag = move,
                    ToolTip = tooltip
                };

                if (move.Index == _selectedIndex) {
                    moveButton.BorderThickness = new Thickness(2);
                    moveButton.BorderBrush = new SolidColorBrush(Colors.Yellow);
                    moveButton.Foreground = Brushes.Yellow;
                }

                moveButton.Click += async (sender, e) => {
                    var buttonSender = (Button)sender;
                    var moveSender = (Move)buttonSender.Tag;
                    _selectedIndex = moveSender.Index;
                    _requiredScore = moveSender.Score;
                    Settings.Default.RequiredScore = _requiredScore;
                    Settings.Default.Save();
                    UpdateRequiredScore();
                    ShowMoves();
                    await AddArrow();
                };

                ToolTipService.SetInitialShowDelay(moveButton, 0);
                Panel.Children.Add(moveButton);
            }
        }
    }

    private async Task RemoveArrow()
    {
        var script = $@"
            var chessBoard = document.querySelector('{ChessBoardTag}');
            if (chessBoard) {{
                var existingSvg = document.getElementById('chezzz-arrow');
                if (existingSvg) {{
                    existingSvg.parentNode.removeChild(existingSvg);
                }}
            }}";

        await WebBrowser.ExecuteScriptAsync(script);
    }

    private async Task AddArrow()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _moves.Count) {
            return;
        }

        var move = _moves[_selectedIndex];
        var src = move.FirstMove[..2];
        var dst = move.FirstMove[2..];
        var x1 = (src[0] - 'a') * 12.5 + 6.25;
        var x2 = (dst[0] - 'a') * 12.5 + 6.25;
        var y1 = ('8' - src[1]) * 12.5 + 6.25;
        var y2 = ('8' - dst[1]) * 12.5 + 6.25;
        if (!isWhite) {
            x1 = 100.0 - x1;
            x2 = 100.0 - x2;
            y1 = 100.0 - y1;
            y2 = 100.0 - y2;
        }

        var dx = x2 - x1;
        var dy = y1 - y2;
        var angle = Math.Round(Math.Atan2(dx, dy) * (180.0 / Math.PI), 2);
        var length = Math.Round(Math.Sqrt(dx * dx + dy * dy), 2);
        const double headRadius = 1.5;
        var point1X = x1 + headRadius;
        var point2Y = y1 - length + headRadius * 2;
        var point3X = x1 + headRadius * 2;
        var point4Y = y1 - length;
        var point5X = x1 - headRadius * 2;
        var point6X = x1 - headRadius;
        var points = $"{point1X},{y1} {point1X},{point2Y} {point3X},{point2Y} {x1},{point4Y} {point5X},{point2Y} {point6X},{point2Y} {point6X},{y1}";
        var svgElement = $"<svg viewBox='0 0 100 100'><polygon transform='rotate({angle} {x1} {y1})' points='{points}' style='fill: rgb(255, 255, 0); opacity: 0.7;' /></svg>";
        var script = $@"
            var existingSvg = document.getElementById('chezzz-arrow');
            if (existingSvg) {{
                existingSvg.parentNode.removeChild(existingSvg);
            }}
            var chessBoard = document.querySelector('{ChessBoardTag}');
            if (chessBoard) {{
                var div = document.createElement('div');
                div.setAttribute('id', 'chezzz-arrow');
                div.setAttribute('style', 'position:relative; pointer-events:none; z-index:9');
                div.innerHTML = `{svgElement}`;
                chessBoard.appendChild(div);
            }}";
        await WebBrowser.CoreWebView2.ExecuteScriptAsync(script);
    }
}
