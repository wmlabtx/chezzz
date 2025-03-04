using System.Text.RegularExpressions;

namespace Chezzz.San;

public class Position : IEquatable<Position>, IComparable<Position>
{
    private static readonly Regex RegexPosition = new("^[a-h][1-8]$", RegexOptions.Compiled);

    public int X { get; set; }
    public int Y { get; set; }

    public bool HasValue => HasValueX & HasValueY;
    public bool HasValueX => X is >= 0 and < 8;
    public bool HasValueY => Y is >= 0 and < 8;
    public Position(string position)
    {
        position = position.ToLower();
        if (!RegexPosition.IsMatch(position)) {
            throw new Exception();
        }

        X = FromFile(position[0]);
        Y = FromRank(position[1]);
    }

    public Position(int y = -1, int x = -1)
    {
        X = x;
        Y = y;
    }

    public static int FromFile(char file)
    {
        if (file is < 'a' or > 'h') {
            throw new Exception();
        }

        return file - 'a';
    }

    public static int FromRank(char rank)
    {
        if (rank is < '1' or > '8') {
            throw new Exception();
        }

        return '8' - rank;
    }

    private char File()
    {
        if (!HasValueX) {
            throw new Exception();
        }

        return (char)(X + 'a');
    }

    private char Rank()
    {
        if (!HasValueY) {
            throw new Exception();
        }

        return (char)('8' - Y);
    }

    public int CompareTo(Position? other)
    {
        return string.Compare(ToString(), other?.ToString(), StringComparison.Ordinal);
    }

    public bool Equals(Position? other)
    {
        return other is not null && X == other.X && Y == other.Y;
    }

    public override string ToString() => File().ToString() + Rank();
}