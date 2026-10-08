namespace StackApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(StackViewModelBase integerStack, StackViewModelBase textStack)
    {
        IntegerStack = integerStack;
        TextStack = textStack;
    }

    public StackViewModelBase IntegerStack { get; }

    public StackViewModelBase TextStack { get; }
}
