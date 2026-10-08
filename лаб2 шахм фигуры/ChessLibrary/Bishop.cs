namespace ChessLibrary;

public class Bishop : ChessPiece
{
    public Bishop(PieceColor color, Position position)
        : base(color, position)
    {
    }

    public override string Name => "Слон";

    public override char Symbol => Color == PieceColor.White ? '♗' : '♝';

    protected override bool CanMoveTo(Position target)
    {
        return IsDiagonalMove(target) && IsPathClear(target);
    }
}
