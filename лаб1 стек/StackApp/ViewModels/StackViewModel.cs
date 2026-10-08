using StackLibrary;

namespace StackApp.ViewModels;

public delegate bool ValueParser<T>(string text, out T value);

public class StackViewModel<T> : StackViewModelBase
{
    private const string EmptyMark = "—";

    private readonly LinkedStack<T> _stack;
    private readonly ValueParser<T> _parser;

    public StackViewModel(string title, string elementTypeName, LinkedStack<T> stack, ValueParser<T> parser)
        : base(title, elementTypeName)
    {
        _stack = stack;
        _parser = parser;
        Refresh();
    }

    protected override void Push()
    {
        string text = InputText.Trim();
        if (!_parser(text, out T value))
        {
            Status = $"«{text}» нельзя добавить: нужен тип {ElementTypeName}";
            return;
        }

        _stack.Push(value);
        InputText = string.Empty;
        Refresh();
        AddHistory($"Push({Format(value)})");
    }

    protected override void Pop()
    {
        T value = _stack.Pop();
        Refresh();
        AddHistory($"Pop() → {Format(value)}");
    }

    protected override void Clear()
    {
        int removed = _stack.Count;
        _stack.Clear();
        Refresh();
        AddHistory($"Clear() — удалено элементов: {removed}");
    }

    private void Refresh()
    {
        string currentText = _stack.IsEmpty ? EmptyMark : Format(_stack.Current);
        ShowState(_stack.Select(Format), _stack.Count, _stack.IsEmpty, currentText);
    }

    private static string Format(T value)
    {
        return value?.ToString() ?? "null";
    }
}
