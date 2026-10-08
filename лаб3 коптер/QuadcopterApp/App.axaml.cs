using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using QuadcopterApp.ViewModels;
using QuadcopterApp.Views;
using QuadcopterLibrary;

namespace QuadcopterApp;

public partial class App : Application
{
    private const int InitialQuadcopterCount = 3;
    private const double InitialGpsFailurePercent = 3;
    private const double InitialFlightSpeed = 90;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindowViewModel viewModel = CreateViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            desktop.Exit += (_, _) => viewModel.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static MainWindowViewModel CreateViewModel()
    {
        MainWindowViewModel viewModel = new(
            new RepairService(),
            new FlightArea(70, 190, 830, 400),
            InitialGpsFailurePercent,
            InitialFlightSpeed);

        for (int i = 0; i < InitialQuadcopterCount; i++)
        {
            viewModel.AddQuadcopter();
        }

        foreach (MechanicTypeOption option in viewModel.MechanicTypes)
        {
            viewModel.HireMechanic(option);
        }

        return viewModel;
    }
}
