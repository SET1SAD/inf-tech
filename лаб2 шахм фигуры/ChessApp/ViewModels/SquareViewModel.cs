using ChessLibrary;

namespace ChessApp.ViewModels;

public class SquareViewModel : ViewModelBase
{
    private string _fillGlyph = string.Empty;
    private string _outlineGlyph = string.Empty;
    private bool _hasPiece;
    private bool _isWhitePiece;
    private bool _isSelected;
    private bool _isAvailable;

    public SquareViewModel(Position position, Action<SquareViewModel> onClick)
    {
        Position = position;
        IsLight = (position.Column + position.Row) % 2 == 1;
        ClickCommand = new RelayCommand(() => onClick(this));
    }

    public Position Position { get; }

    public bool IsLight { get; }

    public bool IsDark => !IsLight;

    public RelayCommand ClickCommand { get; }

    public string FillGlyph
    {
        get => _fillGlyph;
        private set => SetProperty(ref _fillGlyph, value);
    }

    public string OutlineGlyph
    {
        get => _outlineGlyph;
        private set => SetProperty(ref _outlineGlyph, value);
    }

    public bool IsWhitePiece
    {
        get => _isWhitePiece;
        private set => SetProperty(ref _isWhitePiece, value);
    }

    public bool HasPiece
    {
        get => _hasPiece;
        private set
        {
            if (SetProperty(ref _hasPiece, value))
            {
                OnPropertyChanged(nameof(IsMoveTarget));
                OnPropertyChanged(nameof(IsCaptureTarget));
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsAvailable
    {
        get => _isAvailable;
        set
        {
            if (SetProperty(ref _isAvailable, value))
            {
                OnPropertyChanged(nameof(IsMoveTarget));
                OnPropertyChanged(nameof(IsCaptureTarget));
            }
        }
    }

    public bool IsMoveTarget => IsAvailable && !HasPiece;

    public bool IsCaptureTarget => IsAvailable && HasPiece;

    public void ShowPiece(ChessPiece? piece)
    {
        HasPiece = piece is not null;
        IsWhitePiece = piece?.Color == PieceColor.White;

        if (piece is null)
        {
            FillGlyph = string.Empty;
            OutlineGlyph = string.Empty;
        }
        else if (piece.Color == PieceColor.White)
        {
            FillGlyph = ToFilledGlyph(piece.Symbol).ToString();
            OutlineGlyph = piece.Symbol.ToString();
        }
        else
        {
            FillGlyph = piece.Symbol.ToString();
            OutlineGlyph = string.Empty;
        }
    }

    private static char ToFilledGlyph(char whiteSymbol)
    {
        const int OffsetToBlackSymbol = '♛' - '♕';
        return (char)(whiteSymbol + OffsetToBlackSymbol);
    }
}
