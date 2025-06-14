using Chess;
using System.Globalization;
using System.Text;
using System.Windows.Media;

namespace Chezzz.Models;

public class Moves
{
    private const string ARROW_PREFIX = "chezzz";
    private const string OPACITY = "0.7";

    private readonly SortedList<int, Move> moves = [];
    private int selectedIndex = -1;

    public void Clear()
    {
        moves.Clear();
        selectedIndex = -1;
    }

    public void Add(Move move)
    {
        moves[move.Index] = move;
    }

    public Move[] GetMoves()
    {
        return [.. new List<Move>(moves.Values)];
    }

    public Move GetFirstMove()
    {
        return moves.Count > 0 ? moves.First().Value : new Move();
    }

    public void SetSelectedIndex(int index)
    {
        selectedIndex = index;
    }

    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    public int GetCount()
    {
        return moves.Count;
    }

    public int GetDepth()
    {
        return moves.Count > 0 ? moves.First().Value.Depth : 0;
    }

    public Score? GetScore()
    {
        if (moves.Count == 0) {
            return null;
        }

        var move = moves[0];
        return move.Score;
    }

    public Score? GetScoreOpponent()
    {
        if (moves.Count == 0) {
            return null;
        }

        var move = moves[0];
        return new Score(-move.Score.Value, move.Score.IsMate);
    }

    public Score? GetScore(string? previousMove)
    {
        if (string.IsNullOrEmpty(previousMove) || moves.Count == 0) {
            return null;
        }

        var index = -1;
        foreach (var m in moves) {
            if (m.Value.GetSourceAndDestinationSquares().Equals(previousMove[..4], StringComparison.Ordinal)) {
                index = m.Key;
                break;
            }
        }

        if (index == -1) {
            return null;
        }

        var move = moves[index];
        return new Score(-move.Score.Value, move.Score.IsMate);
    }

    public string GetArrowOpponent(string? previousMove, bool isWhite)
    {
        if (string.IsNullOrEmpty(previousMove) || moves.Count == 0) {
            return string.Empty;
        }

        var index = -1;
        foreach (var m in moves) {
            if (m.Value.GetSourceAndDestinationSquares().Equals(previousMove[..4], StringComparison.Ordinal)) {
                index = m.Key;
                break;
            }
        }

        if (index == -1) {
            return string.Empty;
        }

        var move = moves[index];
        var diff = (move.Score - moves[0].Score).Value;
        Color color;
        double normalizedValue;
        if (diff >= -50) {
            normalizedValue = (double)diff / -50;
            color = Helpers.UI.InterpolateColor(Colors.Green, Colors.Gray, normalizedValue);
        }
        else if (diff >= -100) {
            normalizedValue = (double)(diff + 50) / 50;
            color = Helpers.UI.InterpolateColor(Colors.Gray, Colors.Red, normalizedValue);
        }
        else {
            color = Colors.Red;
        }

        var src = move.GetSourceSquare();
        var dst = move.GetDestinationSquare();
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
        var arrow = $"<polygon transform='rotate({angle} {sx1} {sy1})' points='{points}' style='fill:rgb({color.R},{color.G},{color.B}); opacity:{Helpers.UI.OPACITY};' />";
        return arrow;
    }

    public string GetPlayerArrow(bool isWhite)
    {
        var arrow = string.Empty;
        if (moves.TryGetValue(selectedIndex, out Move? move)) {
            var src = move.GetSourceSquare();
            var dst = move.GetDestinationSquare();
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
            arrow = $"<polygon transform='rotate({angle} {sx1} {sy1})' points='{points}' style='fill:rgb(255,255,0); opacity:{Helpers.UI.OPACITY};' />";
        }

        return arrow;
    }

    public void Render(bool isWhite, out string svg, out string style)
    {
        var sbSvg = new StringBuilder();
        var sbStyle = new StringBuilder();
        var groups = GetMoves()
            .OrderByDescending(move => move.Score)
            .GroupBy(move => move.FirstMove[..2])
            .Select(group => group.ToArray())
            .OrderByDescending(list => list.First().Score)
            .ToArray();
        foreach (var group in groups) {
            var bestMove = group.First();
            var bst = bestMove.FirstMove[..2];
            var x1 = (bst[0] - 'a') * 12.5 + 6.25;
            var y1 = ('8' - bst[1]) * 12.5 + 6.25;
            if (!isWhite) {
                x1 = 100.0 - x1;
                y1 = 100.0 - y1;
            }

            var sx1 = Math.Round(x1, 2).ToString(CultureInfo.InvariantCulture);
            var sy1 = Math.Round(y1, 2).ToString(CultureInfo.InvariantCulture);

            sbSvg.Append($@"<circle id='{ARROW_PREFIX}-a{bst}' cx='{sx1}' cy='{sy1}' r='2' style='fill:transparent; stroke:none; stroke-width:0.5; cursor:pointer; pointer-events:auto;' />");
            foreach (var move in group) {
                var color = Helpers.UI.GetColor(move.Score);
                var darkenColor = Helpers.UI.DarkenColor(color, 0.75);

                var src = move.FirstMove.Substring(2, 2);
                x1 = (src[0] - 'a') * 12.5 + 6.25;
                y1 = ('8' - src[1]) * 12.5 + 6.25;
                if (!isWhite) {
                    x1 = 100.0 - x1;
                    y1 = 100.0 - y1;
                }

                sx1 = Math.Round(x1, 2).ToString(CultureInfo.InvariantCulture);
                sy1 = Math.Round(y1, 2).ToString(CultureInfo.InvariantCulture);

                sbSvg.Append($"<circle id='{ARROW_PREFIX}-a{bst}-c{src}' cx='{sx1}' cy='{sy1}' r='4' style='fill:rgb({darkenColor.R},{darkenColor.G},{darkenColor.B}); stroke:rgb({color.R},{color.G},{color.B}); stroke-width:1;' />");
                sbSvg.AppendLine($"<text id='{ARROW_PREFIX}-a{bst}-t{src}' x='{sx1}' y='{sy1}' text-anchor='middle' alignment-baseline='middle' style='font-size:2.5; fill:rgb({color.R},{color.G},{color.B}); font-family:Impact;'>{move.Score}</text>");

                sbStyle.Append($"#{ARROW_PREFIX}-a{bst}:hover ~ #{ARROW_PREFIX}-a{bst}-c{src} {{opacity:{OPACITY};display:block!important;}}");
                sbStyle.Append($"#{ARROW_PREFIX}-a{bst}:hover ~ #{ARROW_PREFIX}-a{bst}-t{src} {{opacity:{OPACITY};display:block!important;}}");
                sbStyle.Append($"#{ARROW_PREFIX}-a{bst}-c{src} {{opacity:{OPACITY};display:block!important;}}");
                sbStyle.Append($"#{ARROW_PREFIX}-a{bst}-t{src} {{opacity:{OPACITY};display:block!important;}}");
                sbStyle.Append($"#{ARROW_PREFIX}-a{bst}-c{src} {{opacity:0;transition: opacity 0.3s ease;}}");
                sbStyle.AppendLine($"#{ARROW_PREFIX}-a{bst}-t{src} {{opacity:0;transition: opacity 0.3s ease;}}");
            }
        }

        svg = sbSvg.ToString();
        style = sbStyle.ToString();
    }

    public static string GetScript(string layer, string chessBoardTag)
    {
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
    var chessBoard = document.querySelector('{chessBoardTag}');
    if(chessBoard){{
        var div = document.getElementById('{ARROW_PREFIX}');
        if(!div){{
            var div = document.createElement('div');
            div.setAttribute('id', '{ARROW_PREFIX}');
            div.setAttribute('style', 'position:relative; pointer-events:none; z-index:9;');
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
        return script;
    }
}