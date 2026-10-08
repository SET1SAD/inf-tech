namespace ChessLibrary;

public abstract class ChessPiece
{
    protected ChessPiece(PieceColor color, Position position)
    {
        Color = color;
        Position = position;
    }

    public event EventHandler<PieceMovedEventArgs>? Moved;

    public PieceColor Color { get; }

    public Position Position { get; private set; }

    public ChessBoard? Board { get; internal set; }

    public abstract string Name { get; }

    public abstract char Symbol { get; }

    public bool MakeMove(Position target)
    {
        if (!IsMoveAllowed(target))
        {
            return false;
        }

        Position from = Position;
        ChessPiece? captured = Board?.GetPieceAt(target);
        if (captured is not null)
        {
            Board!.Remove(captured);
        }

        Position = target;
        OnMoved(new PieceMovedEventArgs(from, target, captured));
        return true;
    }

    public IEnumerable<Position> GetAvailableMoves()
    {
        for (int row = 0; row < Position.BoardSize; row++)
        {
            for (int column = 0; column < Position.BoardSize; column++)
            {
                Position target = new(column, row);
                if (IsMoveAllowed(target))
                {
                    yield return target;
                }
            }
        }
    }

    public override string ToString()
    {
        string colorName = Color == PieceColor.White ? "белые" : "чёрные";
        return $"{Name} ({colorName}) на {Position}";
    }

    protected abstract bool CanMoveTo(Position target);

    protected virtual void OnMoved(PieceMovedEventArgs e)
    {
        Moved?.Invoke(this, e);
    }

    protected bool IsStraightMove(Position target)
    {
        return target.Column == Position.Column || target.Row == Position.Row;
    }

    protected bool IsDiagonalMove(Position target)
    {
        return Math.Abs(target.Column - Position.Column) == Math.Abs(target.Row - Position.Row);
    }

    protected bool IsPathClear(Position target)
    {
        if (Board is null)
        {
            return true;
        }

        int columnStep = Math.Sign(target.Column - Position.Column);
        int rowStep = Math.Sign(target.Row - Position.Row);
        int column = Position.Column + columnStep;
        int row = Position.Row + rowStep;

        while (column != target.Column || row != target.Row)
        {
            if (Board.GetPieceAt(new Position(column, row)) is not null)
            {
                return false;
            }

            column += columnStep;
            row += rowStep;
        }

        return true;
    }

    private bool IsMoveAllowed(Position target)
    {
        if (target == Position || !CanMoveTo(target))
        {
            return false;
        }

        ChessPiece? occupant = Board?.GetPieceAt(target);
        return occupant is null || occupant.Color != Color;
    }
}
