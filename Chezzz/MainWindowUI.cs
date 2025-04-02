using System.Globalization;
using System.Text;
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
        DescreaseTime.IsEnabled = _requiredTime.CanDescreaseValue();
        IncreaseTime.IsEnabled = _requiredTime.CanIncreaseValue();
        var requiredTime = _requiredTime.GetValue();
        var requiredTimeText = $"{requiredTime / 1000.0:F1}s";
        RequiredTimeText.Text = requiredTimeText;
    }

    private void UpdateRequiredScore()
    {
        DescreaseScore.IsEnabled = _requiredScore.CanDescreaseValue();
        IncreaseScore.IsEnabled = _requiredScore.CanIncreaseValue();
        var requiredScore = _requiredScore.GetValue();
        var requiredScoreText = requiredScore switch {
            POSITIVE_MATE => "MAX",
            NEGATIVE_MATE => "MIN",
            _ => requiredScore < 0
                ? $"-{Math.Abs(requiredScore) / 100.0:F2}"
                : $"+{requiredScore / 100.0:F2}"
        };
        RequiredScoreText.Text = requiredScoreText;
    }

    private void ShowMoves()
    {
        var sbSvg = new StringBuilder();
        var sbStyle = new StringBuilder();

        Panel.Children.Clear();

        var groups = _moves
            .Values
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

            var bst = bestMove.FirstMove[..2];
            var x1 = (bst[0] - 'a') * 12.5 + 6.25;
            var y1 = ('8' - bst[1]) * 12.5 + 6.25;
            if (!_isWhite) {
                x1 = 100.0 - x1;
                y1 = 100.0 - y1;
            }

            sbSvg.Append($@"<circle id='{ARROW_PREFIX}-a{bst}' cx='{x1}' cy='{y1}' r='2' fill='transparent' stroke='none' stroke-width='0.5' cursor='pointer' />");

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
                    Foreground = Brushes.White,
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

                if (_selectedMoves.Contains(move.Index)) {
                    moveButton.Content = move.FirstMove.Substring(2, 2);
                    moveButton.Width = 21;
                }
                else {
                    moveButton.Width = 6;
                }

                moveButton.Click += async (sender, e) => {
                    var buttonSender = (Button)sender;
                    var moveSender = (Move)buttonSender.Tag;
                    _selectedIndex = moveSender.Index;
                    _requiredScore.SetValue(moveSender.Score);
                    UpdateRequiredScore();
                    ShowMoves();
                    await AddArrowPlayer();
                };

                ToolTipService.SetInitialShowDelay(moveButton, 0);
                Panel.Children.Add(moveButton);

                var src = move.FirstMove.Substring(2, 2);
                x1 = (src[0] - 'a') * 12.5 + 6.25;
                y1 = ('8' - src[1]) * 12.5 + 6.25;
                if (!_isWhite) {
                    x1 = 100.0 - x1;
                    y1 = 100.0 - y1;
                }

                sbSvg.Append($"<circle id='{ARROW_PREFIX}-a{bst}-c{src}' cx='{x1}' cy='{y1}' r='4' style='fill:black; stroke:rgb({color.R},{color.G},{color.B}); stroke-width:1;' />");
                sbSvg.AppendLine($"<text id='{ARROW_PREFIX}-a{bst}-t{src}' x='{x1}' y='{y1}' text-anchor='middle' alignment-baseline='middle' style='font-size:2.5; fill:rgb({color.R},{color.G},{color.B}); font-family:Impact;'>{move.ScoreText}</text>");

                sbStyle.Append($"#{ARROW_PREFIX}-a{bst}:hover ~ #{ARROW_PREFIX}-a{bst}-c{src} {{opacity:{OPACITY};display:block!important;}}");
                sbStyle.Append($"#{ARROW_PREFIX}-a{bst}:hover ~ #{ARROW_PREFIX}-a{bst}-t{src} {{opacity:{OPACITY};display:block!important;}}");
                sbStyle.Append($"#{ARROW_PREFIX}-a{bst}-c{src} {{opacity:{OPACITY};display:block!important;}}");
                sbStyle.Append($"#{ARROW_PREFIX}-a{bst}-t{src} {{opacity:{OPACITY};display:block!important;}}");
                sbStyle.Append($"#{ARROW_PREFIX}-a{bst}-c{src} {{opacity:0;transition: opacity 0.3s ease;}}");
                sbStyle.AppendLine($"#{ARROW_PREFIX}-a{bst}-t{src} {{opacity:0;transition: opacity 0.3s ease;}}");
            }
        }

        _svg = sbSvg.ToString();
        _style = sbStyle.ToString();
    }

    private string GetArrowOpponent(int index, IReadOnlyDictionary<int, Move> moves)
    {
        var move = moves[index];
        var diff = move.Score - moves[0].Score;
        Color color;
        double normalizedValue;
        if (diff >= -50) {
            normalizedValue = (double)diff / -50;
            color = InterpolateColor(Colors.Green, Colors.Gray, normalizedValue);
        }
        else if (diff >= -100) {
            normalizedValue = (double)(diff + 50) / 50;
            color = InterpolateColor(Colors.Gray, Colors.Red, normalizedValue);
        }
        else {
            color = Colors.Red;
        }
        var darkColor = DarkenColor(color, 0.5);
        var scoreText = diff == 0 ? "BEST" : $"{diff / 100.0:F2}";

        var src = move.FirstMove[..2];
        var dst = move.FirstMove[2..];
        var x1 = (src[0] - 'a') * 12.5 + 6.25;
        var x2 = (dst[0] - 'a') * 12.5 + 6.25;
        var y1 = ('8' - src[1]) * 12.5 + 6.25;
        var y2 = ('8' - dst[1]) * 12.5 + 6.25;
        if (!_isWhite) {
            x1 = 100.0 - x1;
            x2 = 100.0 - x2;
            y1 = 100.0 - y1;
            y2 = 100.0 - y2;
        }

        var dx = x2 - x1;
        var dy = y1 - y2;
        var angle = Math.Round(Math.Atan2(dx, dy) * (180.0 / Math.PI), 2).ToString(CultureInfo.InvariantCulture);
        var length = Math.Round(Math.Sqrt(dx * dx + dy * dy), 2);
        var sx1 = Math.Round(x1, 2).ToString(CultureInfo.InvariantCulture);
        var sy1 = Math.Round(y1, 2).ToString(CultureInfo.InvariantCulture);
        const double headRadius = 1.5;
        var point1X = Math.Round(x1 + headRadius, 2).ToString(CultureInfo.InvariantCulture);
        var point2Y = Math.Round(y1 - length + headRadius * 2, 2).ToString(CultureInfo.InvariantCulture);
        var point3X = Math.Round(x1 + headRadius * 2, 2).ToString(CultureInfo.InvariantCulture);
        var point4Y = Math.Round(y1 - length, 2).ToString(CultureInfo.InvariantCulture);
        var point5X = Math.Round(x1 - headRadius * 2, 2).ToString(CultureInfo.InvariantCulture);
        var point6X = Math.Round(x1 - headRadius, 2).ToString(CultureInfo.InvariantCulture);
        var points = $"{point1X},{sy1} {point1X},{point2Y} {point3X},{point2Y} {sx1},{point4Y} {point5X},{point2Y} {point6X},{point2Y} {point6X},{sy1}";
        var arrow = $"<polygon transform='rotate({angle} {sx1} {sy1})' points='{points}' style='fill:rgb({color.R},{color.G},{color.B}); opacity:{OPACITY};' />";
        return arrow;
    }

    private async Task AddArrowPlayer()
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
        if (!_isWhite) {
            x1 = 100.0 - x1;
            x2 = 100.0 - x2;
            y1 = 100.0 - y1;
            y2 = 100.0 - y2;
        }

        var dx = x2 - x1;
        var dy = y1 - y2;
        var angle = Math.Round(Math.Atan2(dx, dy) * (180.0 / Math.PI), 2).ToString(CultureInfo.InvariantCulture);
        var length = Math.Round(Math.Sqrt(dx * dx + dy * dy), 2);
        var sx1 = Math.Round(x1, 2).ToString(CultureInfo.InvariantCulture);
        var sy1 = Math.Round(y1, 2).ToString(CultureInfo.InvariantCulture);
        const double headRadius = 1.5;
        var point1X = Math.Round(x1 + headRadius, 2).ToString(CultureInfo.InvariantCulture);
        var point2Y = Math.Round(y1 - length + headRadius * 2, 2).ToString(CultureInfo.InvariantCulture);
        var point3X = Math.Round(x1 + headRadius * 2, 2).ToString(CultureInfo.InvariantCulture);
        var point4Y = Math.Round(y1 - length, 2).ToString(CultureInfo.InvariantCulture);
        var point5X = Math.Round(x1 - headRadius * 2, 2).ToString(CultureInfo.InvariantCulture);
        var point6X = Math.Round(x1 - headRadius, 2).ToString(CultureInfo.InvariantCulture);
        var points = $"{point1X},{sy1} {point1X},{point2Y} {point3X},{point2Y} {sx1},{point4Y} {point5X},{point2Y} {point6X},{point2Y} {point6X},{sy1}";
        var playerArrow = $"<polygon transform='rotate({angle} {sx1} {sy1})' points='{points}' style='fill:rgb(255,255,0); opacity:{OPACITY};' />";
        var layer = $"<svg viewBox='0 0 100 100'>{playerArrow}{_opponentArrow}{_svg}</svg><style>{_style}</style>";
        var script = $@"
(function(){{
    if(window._chessBoardObserver){{
        window._chessBoardObserver.disconnect();
        window._chessBoardObserver = null;
    }}
    window._disableArrowObserver = true;
    function removeArrow(){{
        existingArrow = document.getElementById('{ARROW_PREFIX}');
        if(existingArrow){{
            existingArrow.parentNode.removeChild(existingArrow);
        }}
    }}
    var chessBoard = document.querySelector('{_chessBoardTag}');
    if(chessBoard){{
        var div = document.getElementById('{ARROW_PREFIX}');
        if(!div){{
            var div = document.createElement('div');
            div.setAttribute('id', '{ARROW_PREFIX}');
            div.setAttribute('style', 'position:relative; z-index:9;');
            chessBoard.appendChild(div);
        }}
        div.innerHTML = `{layer}`;
    }}
    setTimeout(function(){{
        window._disableArrowObserver = false;
        window._chessBoardObserver = new MutationObserver(function(mutations){{
            if(window._disableArrowObserver){{
                return;
            }}
            mutations.forEach(function(mutation){{
                if(mutation.type === 'childList' || mutation.type === 'attributes'){{
                    removeArrow();
                    window._disableArrowObserver = true;
                }}
            }});
        }});
        if(chessBoard){{
            window._chessBoardObserver.observe(chessBoard, {{
                childList: true,
                attributes: true,
                subtree: true
            }});
        }}
    }}, 0);
}})();";
        await WebBrowser.CoreWebView2.ExecuteScriptAsync(script);
    }
}