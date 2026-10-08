namespace ChessLibrary;

public class Rook : ChessPiece
{
    public Rook(PieceColor color, Position position)
        : base(color, position)
    {
    }

    public override string Name => "Ладья";

    public override char Symbol => Color == PieceColor.White ? '♖' : '♜';

    protected override bool CanMoveTo(Position target)
    {
        return IsStraightMove(target) && IsPathClear(target);
    }
}
