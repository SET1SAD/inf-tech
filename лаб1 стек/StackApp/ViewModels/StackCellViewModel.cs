namespace StackApp.ViewModels;

public class StackCellViewModel
{
    public StackCellViewModel(string text, bool isTop)
    {
        Text = text;
        IsTop = isTop;
    }

    public string Text { get; }

    public bool IsTop { get; }
}
