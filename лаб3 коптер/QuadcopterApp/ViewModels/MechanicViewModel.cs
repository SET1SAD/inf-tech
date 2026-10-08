using Avalonia.Media;
using QuadcopterLibrary;

namespace QuadcopterApp.ViewModels;

public class MechanicViewModel : ViewModelBase
{
    private const double FigureCenterX = 40;
    private const double FigureFeetY = 46;

    private double _left;
    private double _top;
    private string _activityText = string.Empty;
    private bool _isWalking;
    private bool _isRepairing;
    private bool _isAway;
    private int _repairsCompleted;

    public MechanicViewModel(IMechanic mechanic, IBrush accent)
    {
        Mechanic = mechanic;
        Accent = accent;
        Refresh();
    }

    public IMechanic Mechanic { get; }

    public IBrush Accent { get; }

    public string Name => Mechanic.Name;

    public string Specialization => Mechanic.Specialization;

    public double Left
    {
        get => _left;
        private set => SetProperty(ref _left, value);
    }

    public double Top
    {
        get => _top;
        private set => SetProperty(ref _top, value);
    }

    public string ActivityText
    {
        get => _activityText;
        private set => SetProperty(ref _activityText, value);
    }

    public bool IsWalking
    {
        get => _isWalking;
        private set => SetProperty(ref _isWalking, value);
    }

    public bool IsRepairing
    {
        get => _isRepairing;
        private set => SetProperty(ref _isRepairing, value);
    }

    public bool IsAway
    {
        get => _isAway;
        private set => SetProperty(ref _isAway, value);
    }

    public int RepairsCompleted
    {
        get => _repairsCompleted;
        private set => SetProperty(ref _repairsCompleted, value);
    }

    public void Refresh()
    {
        Left = Mechanic.X - FigureCenterX;
        Top = Mechanic.Y - FigureFeetY;

        MechanicActivity activity = Mechanic.Activity;
        Quadcopter? target = Mechanic.Target;
        ActivityText = target is null
            ? activity.GetDescription()
            : $"{activity.GetDescription()}: {target.Name}";
        IsWalking = activity is MechanicActivity.GoingToQuadcopter or MechanicActivity.Returning;
        IsRepairing = activity == MechanicActivity.Repairing;
        IsAway = activity != MechanicActivity.AtBase;
        RepairsCompleted = Mechanic.RepairsCompleted;
    }
}
