namespace ChessLibrary;

public readonly record struct Position
{
    public const int BoardSize = 8;

    public Position(int column, int row)
    {
        if (!IsInside(column, row))
        {
            throw new ArgumentOutOfRangeException(nameof(column), "Клетка находится за пределами доски.");
        }

        Column = column;
        Row = row;
    }

    public int Column { get; }

    public int Row { get; }

    public static bool IsInside(int column, int row)
    {
        return column >= 0 && column < BoardSize && row >= 0 && row < BoardSize;
    }

    public static bool TryParse(string? text, out Position position)
    {
        position = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string value = text.Trim().ToLowerInvariant();
        if (value.Length != 2)
        {
            return false;
        }

        int column = value[0] - 'a';
        int row = value[1] - '1';
        if (!IsInside(column, row))
        {
            return false;
        }

        position = new Position(column, row);
        return true;
    }

    public override string ToString()
    {
        return $"{(char)('a' + Column)}{Row + 1}";
    }
}
