namespace ChessLibrary;

public class Queen : ChessPiece
{
    public Queen(PieceColor color, Position position)
        : base(color, position)
    {
    }

    public override string Name => "Ферзь";

    public override char Symbol => Color == PieceColor.White ? '♕' : '♛';

    protected override bool CanMoveTo(Position target)
    {
        return (IsStraightMove(target) || IsDiagonalMove(target)) && IsPathClear(target);
    }
}
