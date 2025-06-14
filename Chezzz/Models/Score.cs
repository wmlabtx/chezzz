namespace Chezzz.Models;

public class Score(int value, bool isMate) : IComparable<Score>
{
    public readonly int Value = value;
    public readonly bool IsMate = isMate;

    public override string ToString()
    {
        if (IsMate) {
            return Value >= 0 ? $"+M{Value}" : $"-M{-Value}";
        }

        return Value >= 0 ? $"+{Value / 100.0:F2}" : $"{Value / 100.0:F2}";
    }

    // +M1,+M2,...+M20,+1000,+150,0,-150,-1000,-M20,...-M2,-M1
    int IComparable<Score>.CompareTo(Score? other)
    {
        if (other is null) {
            return 1;
        }

        if (IsMate && Value >= 0 && other.IsMate && other.Value >= 0) {
            return -Value.CompareTo(other.Value);
        }
        else if (IsMate && Value >= 0 && !other.IsMate) {
            return 1;
        }
        else if (IsMate && Value >= 0 && other.IsMate && other.Value < 0) {
            return 1;
        }

        else if (!IsMate && other.IsMate && other.Value >= 0) {
            return -1;
        }
        else if (!IsMate && !other.IsMate) {
            return Value.CompareTo(other.Value);
        }
        else if (!IsMate && other.IsMate && other.Value < 0) {
            return 1;
        }

        else if (IsMate && Value < 0 && other.IsMate && other.Value >= 0) {
            return -1;
        }
        else if (IsMate && Value < 0 && !other.IsMate) {
            return -1;
        }
        else if (IsMate && Value < 0 && other.IsMate && other.Value < 0) {
            return -Value.CompareTo(other.Value);
        }

        return 0;
    }

    public static Score operator -(Score s1, Score s2)
    {
        var diff = 0;
        if (s1.IsMate && s1.Value >= 0 && s2.IsMate && s2.Value >= 0) {
            diff = s2.Value - s1.Value;
        }
        else if (s1.IsMate && s1.Value >= 0 && !s2.IsMate) {
            diff = 10000;
        }
        else if (s1.IsMate && s1.Value >= 0 && s2.IsMate && s2.Value < 0) {
            diff = 10000;
        }

        else if (!s1.IsMate && s2.IsMate && s2.Value >= 0) {
            diff = -10000;
        }
        else if (!s1.IsMate && !s2.IsMate) {
            diff = s1.Value - s2.Value;
        }
        else if (!s1.IsMate && s2.IsMate && s2.Value < 0) {
            diff = 10000;
        }

        else if (s1.IsMate && s1.Value < 0 && s2.IsMate && s2.Value >= 0) {
            diff = -10000;
        }
        else if (s1.IsMate && s1.Value < 0 && !s2.IsMate) {
            diff = -10000;
        }
        else if (s1.IsMate && s1.Value < 0 && s2.IsMate && s2.Value < 0) {
            diff = s2.Value - s1.Value;
        }

        return new Score(diff, false);
    }
}