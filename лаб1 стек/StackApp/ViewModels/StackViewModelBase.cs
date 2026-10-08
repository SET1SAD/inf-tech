using System.Collections.ObjectModel;

namespace StackApp.ViewModels;

public abstract class StackViewModelBase : ViewModelBase
{
    private const int MaxHistoryLength = 50;

    private string _inputText = string.Empty;
    private string _status = string.Empty;
    private string _currentText = string.Empty;
    private int _count;
    private bool _isEmpty = true;

    protected StackViewModelBase(string title, string elementTypeName)
    {
        Title = title;
        ElementTypeName = elementTypeName;
        PushCommand = new RelayCommand(Push);
        PopCommand = new RelayCommand(Pop, () => !IsEmpty);
        ClearCommand = new RelayCommand(Clear, () => !IsEmpty);
    }

    public string Title { get; }

    public string ElementTypeName { get; }

    public ObservableCollection<StackCellViewModel> Cells { get; } = new();

    public ObservableCollection<string> History { get; } = new();

    public RelayCommand PushCommand { get; }

    public RelayCommand PopCommand { get; }

    public RelayCommand ClearCommand { get; }

    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    public string Status
    {
        get => _status;
        protected set => SetProperty(ref _status, value);
    }

    public string CurrentText
    {
        get => _currentText;
        private set => SetProperty(ref _currentText, value);
    }

    public int Count
    {
        get => _count;
        private set => SetProperty(ref _count, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set
        {
            if (SetProperty(ref _isEmpty, value))
            {
                OnPropertyChanged(nameof(IsEmptyText));
            }
        }
    }

    public string IsEmptyText => IsEmpty ? "да" : "нет";

    protected abstract void Push();

    protected abstract void Pop();

    protected abstract void Clear();

    protected void ShowState(IEnumerable<string> valuesFromTop, int count, bool isEmpty, string currentText)
    {
        Cells.Clear();
        bool isTop = true;
        foreach (string value in valuesFromTop)
        {
            Cells.Add(new StackCellViewModel(value, isTop));
            isTop = false;
        }

        Count = count;
        IsEmpty = isEmpty;
        CurrentText = currentText;
        PopCommand.RaiseCanExecuteChanged();
        ClearCommand.RaiseCanExecuteChanged();
    }

    protected void AddHistory(string record)
    {
        History.Insert(0, record);
        if (History.Count > MaxHistoryLength)
        {
            History.RemoveAt(History.Count - 1);
        }

        Status = record;
    }
}
