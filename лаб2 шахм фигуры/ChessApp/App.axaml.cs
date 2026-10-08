using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ChessApp.ViewModels;
using ChessApp.Views;
using ChessLibrary;

namespace ChessApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = CreateViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static MainWindowViewModel CreateViewModel()
    {
        PieceKindOption[] pieceKinds =
        {
            new("Ферзь", (color, position) => new Queen(color, position)),
            new("Ладья", (color, position) => new Rook(color, position)),
            new("Слон", (color, position) => new Bishop(color, position))
        };

        ColorOption[] pieceColors =
        {
            new("Белые", PieceColor.White),
            new("Чёрные", PieceColor.Black)
        };

        return new MainWindowViewModel(new ChessBoard(), pieceKinds, pieceColors, CreateInitialPieces, "e4");
    }

    private static IEnumerable<ChessPiece> CreateInitialPieces()
    {
        return new ChessPiece[]
        {
            new Rook(PieceColor.White, new Position(0, 0)),
            new Bishop(PieceColor.White, new Position(2, 0)),
            new Queen(PieceColor.White, new Position(3, 0)),
            new Bishop(PieceColor.White, new Position(5, 0)),
            new Rook(PieceColor.White, new Position(7, 0)),
            new Rook(PieceColor.Black, new Position(0, 7)),
            new Bishop(PieceColor.Black, new Position(2, 7)),
            new Queen(PieceColor.Black, new Position(3, 7)),
            new Bishop(PieceColor.Black, new Position(5, 7)),
            new Rook(PieceColor.Black, new Position(7, 7))
        };
    }
}
