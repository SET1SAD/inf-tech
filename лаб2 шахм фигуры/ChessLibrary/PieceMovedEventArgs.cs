namespace ChessLibrary;

public class PieceMovedEventArgs : EventArgs
{
    public PieceMovedEventArgs(Position from, Position to, ChessPiece? capturedPiece)
    {
        From = from;
        To = to;
        CapturedPiece = capturedPiece;
    }

    public Position From { get; }

    public Position To { get; }

    public ChessPiece? CapturedPiece { get; }
}
