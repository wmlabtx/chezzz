namespace Chezzz.San;

public class Piece
{
    public char Type { get; }
    public char Color { get; }

    public Piece(char color = '.', char type = '.')
    {
        Color = color;
        Type = type;
    }

    public char ToFen()
    {
        return Color == 'w' ? char.ToUpper(Type) : Type;
    }
}