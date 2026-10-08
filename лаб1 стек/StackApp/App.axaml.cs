using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using StackApp.ViewModels;
using StackApp.Views;
using StackLibrary;

namespace StackApp;

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
        StackViewModel<int> integerStack = new(
            "Стек целых чисел",
            "int",
            new LinkedStack<int>(new[] { 5, 12, 40 }),
            int.TryParse);

        StackViewModel<string> textStack = new(
            "Стек строк",
            "string",
            new LinkedStack<string>(new[] { "первый", "второй" }),
            TryParseText);

        return new MainWindowViewModel(integerStack, textStack);
    }

    private static bool TryParseText(string text, out string value)
    {
        value = text;
        return text.Length > 0;
    }
}
