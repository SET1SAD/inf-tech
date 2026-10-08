using System.Collections.ObjectModel;
using ChessLibrary;

namespace ChessApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private const int MaxLogLength = 100;

    private readonly ChessBoard _board;
    private readonly Func<IEnumerable<ChessPiece>> _createInitialPieces;
    private ChessPiece? _selectedPiece;
    private PieceKindOption _selectedKind;
    private ColorOption _selectedColor;
    private string _newPieceSquare;
    private string _status = string.Empty;
    private string _selectedPieceText = string.Empty;
    private string _selectedPieceClass = string.Empty;
    private string _availableMovesText = string.Empty;

    public MainWindowViewModel(
        ChessBoard board,
        IReadOnlyList<PieceKindOption> pieceKinds,
        IReadOnlyList<ColorOption> pieceColors,
        Func<IEnumerable<ChessPiece>> createInitialPieces,
        string newPieceSquare)
    {
        _board = board;
        _createInitialPieces = createInitialPieces;
        _newPieceSquare = newPieceSquare;
        PieceKinds = pieceKinds;
        PieceColors = pieceColors;

        for (int row = Position.BoardSize - 1; row >= 0; row--)
        {
            for (int column = 0; column < Position.BoardSize; column++)
            {
                Squares.Add(new SquareViewModel(new Position(column, row), OnSquareClicked));
            }
        }

        _selectedKind = PieceKinds[0];
        _selectedColor = PieceColors[0];

        AddPieceCommand = new RelayCommand(AddPiece);
        RemovePieceCommand = new RelayCommand(RemoveSelectedPiece, () => _selectedPiece is not null);
        ClearBoardCommand = new RelayCommand(ClearBoard);
        ResetBoardCommand = new RelayCommand(PlaceInitialPieces);

        PlaceInitialPieces();
    }

    public ObservableCollection<SquareViewModel> Squares { get; } = new();

    public ObservableCollection<string> MoveLog { get; } = new();

    public IReadOnlyList<PieceKindOption> PieceKinds { get; }

    public IReadOnlyList<ColorOption> PieceColors { get; }

    public IReadOnlyList<string> ColumnLabels { get; } = new[] { "a", "b", "c", "d", "e", "f", "g", "h" };

    public IReadOnlyList<string> RowLabels { get; } = new[] { "8", "7", "6", "5", "4", "3", "2", "1" };

    public RelayCommand AddPieceCommand { get; }

    public RelayCommand RemovePieceCommand { get; }

    public RelayCommand ClearBoardCommand { get; }

    public RelayCommand ResetBoardCommand { get; }

    public PieceKindOption SelectedKind
    {
        get => _selectedKind;
        set => SetProperty(ref _selectedKind, value);
    }

    public ColorOption SelectedColor
    {
        get => _selectedColor;
        set => SetProperty(ref _selectedColor, value);
    }

    public string NewPieceSquare
    {
        get => _newPieceSquare;
        set => SetProperty(ref _newPieceSquare, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string SelectedPieceText
    {
        get => _selectedPieceText;
        private set => SetProperty(ref _selectedPieceText, value);
    }

    public string SelectedPieceClass
    {
        get => _selectedPieceClass;
        private set => SetProperty(ref _selectedPieceClass, value);
    }

    public string AvailableMovesText
    {
        get => _availableMovesText;
        private set => SetProperty(ref _availableMovesText, value);
    }

    public bool HasSelection => _selectedPiece is not null;

    private void OnSquareClicked(SquareViewModel square)
    {
        ChessPiece? piece = _board.GetPieceAt(square.Position);

        if (_selectedPiece is not null)
        {
            if (piece == _selectedPiece)
            {
                SelectPiece(null);
                Status = "Выбор снят";
                return;
            }

            bool isOwnPiece = piece is not null && piece.Color == _selectedPiece.Color;
            if (piece is null || (!isOwnPiece && square.IsAvailable))
            {
                TryMove(_selectedPiece, square.Position);
                return;
            }
        }

        if (piece is not null)
        {
            SelectPiece(piece);
            Status = $"Выбрана фигура: {piece}";
        }
        else
        {
            NewPieceSquare = square.Position.ToString();
            Status = $"Клетка {square.Position} пуста";
        }
    }

    private void TryMove(ChessPiece piece, Position target)
    {
        Position from = piece.Position;
        if (piece.MakeMove(target))
        {
            SelectPiece(null);
            Status = $"Ход выполнен: {Describe(piece)} {from} → {target}";
        }
        else
        {
            Status = $"Ход невозможен: {Describe(piece)} {from} → {target}";
            AddLog(Status);
        }
    }

    private void OnPieceMoved(object? sender, PieceMovedEventArgs e)
    {
        if (sender is not ChessPiece piece)
        {
            return;
        }

        string record = $"{Describe(piece)} {e.From} → {e.To}";
        if (e.CapturedPiece is not null)
        {
            e.CapturedPiece.Moved -= OnPieceMoved;
            record += $", взята фигура: {Describe(e.CapturedPiece)}";
        }

        AddLog(record);
        RefreshBoard();
    }

    private void AddPiece()
    {
        if (!Position.TryParse(NewPieceSquare, out Position position))
        {
            Status = "Клетка должна быть указана в формате буква+цифра, например e4";
            return;
        }

        ChessPiece piece = SelectedKind.Create(SelectedColor.Color, position);
        if (!_board.Add(piece))
        {
            Status = $"Клетка {position} уже занята";
            return;
        }

        piece.Moved += OnPieceMoved;
        AddLog($"Поставлена фигура: {piece}");
        Status = $"Поставлена фигура: {piece}";
        RefreshBoard();
    }

    private void RemoveSelectedPiece()
    {
        if (_selectedPiece is null)
        {
            return;
        }

        ChessPiece piece = _selectedPiece;
        piece.Moved -= OnPieceMoved;
        _board.Remove(piece);
        SelectPiece(null);
        AddLog($"Убрана фигура: {piece}");
        Status = $"Убрана фигура: {piece}";
    }

    private void ClearBoard()
    {
        foreach (ChessPiece piece in _board.Pieces)
        {
            piece.Moved -= OnPieceMoved;
        }

        _board.Clear();
        SelectPiece(null);
        AddLog("Доска очищена");
        Status = "Доска очищена";
    }

    private void PlaceInitialPieces()
    {
        ClearBoard();
        MoveLog.Clear();

        foreach (ChessPiece piece in _createInitialPieces())
        {
            if (_board.Add(piece))
            {
                piece.Moved += OnPieceMoved;
            }
        }

        AddLog($"Начальная расстановка: фигур на доске — {_board.Pieces.Count}");
        Status = "Выберите фигуру";
        RefreshBoard();
    }

    private void SelectPiece(ChessPiece? piece)
    {
        _selectedPiece = piece;

        if (piece is null)
        {
            SelectedPieceText = "Фигура не выбрана";
            SelectedPieceClass = string.Empty;
            AvailableMovesText = string.Empty;
        }
        else
        {
            List<Position> moves = piece.GetAvailableMoves().ToList();
            SelectedPieceText = piece.ToString();
            SelectedPieceClass = $"Класс: {piece.GetType().Name}, базовый класс: {piece.GetType().BaseType?.Name}";
            AvailableMovesText = moves.Count == 0
                ? "Доступных ходов нет"
                : $"Доступные ходы ({moves.Count}): {string.Join(", ", moves)}";
        }

        OnPropertyChanged(nameof(HasSelection));
        RemovePieceCommand.RaiseCanExecuteChanged();
        RefreshBoard();
    }

    private void RefreshBoard()
    {
        HashSet<Position> available = _selectedPiece is null
            ? new HashSet<Position>()
            : _selectedPiece.GetAvailableMoves().ToHashSet();

        foreach (SquareViewModel square in Squares)
        {
            ChessPiece? piece = _board.GetPieceAt(square.Position);
            square.ShowPiece(piece);
            square.IsSelected = piece is not null && piece == _selectedPiece;
            square.IsAvailable = available.Contains(square.Position);
        }
    }

    private static string Describe(ChessPiece piece)
    {
        string colorName = piece.Color == PieceColor.White ? "белые" : "чёрные";
        return $"{piece.Name} ({colorName})";
    }

    private void AddLog(string record)
    {
        MoveLog.Insert(0, record);
        if (MoveLog.Count > MaxLogLength)
        {
            MoveLog.RemoveAt(MoveLog.Count - 1);
        }
    }
}
