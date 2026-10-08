using ChessLibrary;

namespace ChessApp.ViewModels;

public delegate ChessPiece PieceFactory(PieceColor color, Position position);

public sealed record PieceKindOption(string Name, PieceFactory Create);

public sealed record ColorOption(string Name, PieceColor Color);
