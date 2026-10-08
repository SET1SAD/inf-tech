namespace ChessLibrary;

public class ChessBoard
{
    private readonly List<ChessPiece> _pieces = new();

    public IReadOnlyList<ChessPiece> Pieces => _pieces;

    public ChessPiece? GetPieceAt(Position position)
    {
        return _pieces.Find(piece => piece.Position == position);
    }

    public bool Add(ChessPiece piece)
    {
        ArgumentNullException.ThrowIfNull(piece);

        if (piece.Board is not null || GetPieceAt(piece.Position) is not null)
        {
            return false;
        }

        piece.Board = this;
        _pieces.Add(piece);
        return true;
    }

    public bool Remove(ChessPiece piece)
    {
        if (!_pieces.Remove(piece))
        {
            return false;
        }

        piece.Board = null;
        return true;
    }

    public void Clear()
    {
        foreach (ChessPiece piece in _pieces)
        {
            piece.Board = null;
        }

        _pieces.Clear();
    }
}
