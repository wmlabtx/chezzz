using Chezzz.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Move = Chezzz.Models.Move;

namespace Chezzz;

public partial class MainWindow
{
    private async void UpdateRequiredScore()
    {
        DescreaseScore.IsEnabled = _requiredScore.CanDescreaseValue();
        IncreaseScore.IsEnabled = _requiredScore.CanIncreaseValue();
        var requiredScore = _requiredScore.GetValue();
        var requiredScoreText = requiredScore switch {
            POSITIVE_MATE => "MAX",
            NEGATIVE_MATE => "MIN",
            < 0 => $"-{Math.Abs(requiredScore) / 100.0:F2}",
            _ => $"+{requiredScore / 100.0:F2}"
        };

        RequiredScoreText.Text = requiredScoreText;

        _moves.SetSelectedIndex(-1);
        if (_moves.GetCount() > 0) {
            ShowMoves(true);
            await AddArrowPlayer();
        }
    }

    private void UpdateRequiredTime()
    {
        DescreaseTime.IsEnabled = _requiredTime.CanDescreaseValue();
        IncreaseTime.IsEnabled = _requiredTime.CanIncreaseValue();
        var requiredTime = _requiredTime.GetValue();
        var requiredTimeText = $"{requiredTime / 1000.0:F1}s";
        RequiredTimeText.Text = requiredTimeText;
    }

    private void ShowMoves(bool auto)
    {
        if (_currentScore == null) {
            return;
        }

        _moves.Render(_isWhite, out string svg, out string style);
        _svg = svg;
        _style = style;

        Panel.Children.Clear();

        var bestMoves = new List<Move>();
        var requiredScoreValue = _requiredScore.GetValue();
        var requiredScore = requiredScoreValue switch {
            POSITIVE_MATE => new Score(1, true),
            NEGATIVE_MATE => new Score(-1, true),
            _ => new Score(requiredScoreValue, false),
        };

        var medianScore = requiredScore;
        if (!requiredScore.IsMate && !_currentScore.IsMate) {
            medianScore = new Score((requiredScoreValue + _currentScore.Value) / 2, false);
        }

        var betterMoves = new List<Move>();
        var equialMoves = new List<Move>();
        var worseMoves = new List<Move>();

        var moves = new List<Move>(_moves.GetMoves().Where(e => !string.IsNullOrEmpty(e.Opening)));
        if (moves.Count == 0) {
            moves = [.. _moves.GetMoves()];
        }

        foreach (var move in moves) {
            var diff = (move.Score - medianScore).Value;
            if (diff > 0) {
                betterMoves.Add(move);
            }
            else if (diff == 0) {
                equialMoves.Add(move);
            }
            else {
                worseMoves.Add(move);
            }
        }

        betterMoves.Reverse();

        bestMoves.AddRange(equialMoves.Take(1));
        if (bestMoves.Count < 1 && betterMoves.Count > 0) {
            bestMoves.Add(betterMoves[0]);
            betterMoves.RemoveAt(0);
        }

        if (bestMoves.Count < 1 && worseMoves.Count > 0) {
            bestMoves.Add(worseMoves[0]);
            worseMoves.RemoveAt(0);
        }

        var moveScore = new Move {
            Index = -1,
            Score = _currentScore
        };

        bestMoves.Add(moveScore);
        bestMoves = [.. bestMoves.OrderByDescending(e => e.Score)];

        foreach (var move in bestMoves) {
            var color = Helpers.UI.GetColor(move.Score);
            var darkenColor = Helpers.UI.DarkenColor(color, 0.5);
            var ligthenColor = Helpers.UI.LigthenColor(color, 0.5);
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

            if (move.Index == -1) {
                var scoreLabel = new Label {
                    Content = move.Score.ToString(),
                    Margin = new Thickness(2, 0, 0, 0),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(color),
                    Foreground = new SolidColorBrush(color),
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };

                Panel.Children.Add(scoreLabel);
                continue;
            }

            var foregroundColor = string.IsNullOrEmpty(move.Opening) ? Brushes.White : Brushes.LightGreen;
            if (move.Index == _moves.GetSelectedIndex()) {
                foregroundColor = Brushes.Yellow;
            }

            var grid = new Grid {
                Margin = new Thickness(4, 0, 4, 0)
            };

            var gridTooltip = new Grid {
                Margin = new Thickness(4, 0, 4, 0)
            };

            var column = 0;
            var columnTooltip = 0;

            var scoreTextBlock = new TextBlock {
                Text = move.Score.ToString(),
                Foreground = foregroundColor,
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

            gridTooltip.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            wdlTextBlock.SetValue(Grid.ColumnProperty, columnTooltip++);
            gridTooltip.Children.Add(wdlTextBlock);

            var firstMoveTextBlock = new TextBlock {
                Text = $"{move.FirstPiece}{move.FirstMove}",
                Foreground = foregroundColor,
                Padding = new Thickness(4, 0, 0, 0)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            firstMoveTextBlock.SetValue(Grid.ColumnProperty, columnTooltip++);
            grid.Children.Add(firstMoveTextBlock);

            if (!string.IsNullOrEmpty(move.Opening)) {
                var openingTextBlock = new TextBlock {
                    Text = move.Opening,
                    Foreground = Brushes.White,
                    Padding = new Thickness(4, 0, 0, 0)
                };
                gridTooltip.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                openingTextBlock.SetValue(Grid.ColumnProperty, columnTooltip);
                gridTooltip.Children.Add(openingTextBlock);
            }
            else if (!string.IsNullOrEmpty(move.SecondMove)) {
                var secondMoveTextBlock = new TextBlock {
                    Text = $"\u2026{move.SecondPiece}{move.SecondMove}?",
                    Foreground = Brushes.White,
                    Padding = new Thickness(4, 0, 0, 0)
                };
                gridTooltip.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                secondMoveTextBlock.SetValue(Grid.ColumnProperty, columnTooltip);
                gridTooltip.Children.Add(secondMoveTextBlock);
            }

            var tooltip = new ToolTip {
                Content = gridTooltip,
                Background = gradient
            };

            var moveButton = new Button {
                Content = grid,
                Margin = new Thickness(2, 0, 0, 0),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(color),
                Background = gradient,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Cursor = Cursors.Hand,
                Tag = move,
                ToolTip = tooltip
            };

            if (move.Index == _moves.GetSelectedIndex()) {
                moveButton.BorderThickness = new Thickness(2);
                moveButton.BorderBrush = new SolidColorBrush(Colors.Yellow);
            }

            moveButton.Click += async (sender, e) => {
                var buttonSender = (Button)sender;
                var moveSender = (Move)buttonSender.Tag;
                var selectedIndex = moveSender.Index == _moves.GetSelectedIndex() ? -1 : moveSender.Index;
                _moves.SetSelectedIndex(selectedIndex);
                ShowMoves(false);
                await AddArrowPlayer();
            };

            ToolTipService.SetInitialShowDelay(moveButton, 0);
            Panel.Children.Add(moveButton);
        }
    }

    private async Task AddArrowPlayer()
    {
        var playerArrow = _moves.GetPlayerArrow(_isWhite);
        var layer = $"<svg viewBox='0 0 100 100'>{playerArrow}{_opponentArrow}{_svg}</svg><style>{_style}</style>";
        var script = Models.Moves.GetScript(layer:layer, chessBoardTag:_chessBoardTag);
        await WebBrowser.CoreWebView2.ExecuteScriptAsync(script);
    }
}