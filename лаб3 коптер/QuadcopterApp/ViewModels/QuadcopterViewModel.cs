using Avalonia;
using Avalonia.Media;
using QuadcopterLibrary;

namespace QuadcopterApp.ViewModels;

public class QuadcopterViewModel : ViewModelBase
{
    private const double BodyCenterX = 55;
    private const double BodyCenterY = 52;
    private const double PadRadius = 26;
    private const double OperatorCenterX = 30;
    private const double OperatorFeetY = 46;
    private const double AntennaOffsetX = 16;
    private const double AntennaOffsetY = 32;
    private const double ShadowBaseWidth = 46;
    private const double MaxRepairBarWidth = 48;

    private double _left;
    private double _top;
    private double _shadowLeft;
    private double _shadowTop;
    private double _shadowWidth;
    private double _shadowOpacity;
    private double _repairBarWidth;
    private string _stateText = string.Empty;
    private string _gpsText = string.Empty;
    private string _remoteText = string.Empty;
    private string _remoteButtonText = string.Empty;
    private bool _isSpinning;
    private bool _isGpsAlarm;
    private bool _isEmergency;
    private bool _isRepairing;
    private bool _isRemoteOn;
    private bool _hasLink;
    private bool _canFailGps;
    private Point _linkStart;
    private Point _linkEnd;
    private int _flightCount;
    private int _gpsFailureCount;

    public QuadcopterViewModel(Quadcopter quadcopter, Operator remoteOperator, IBrush accent)
    {
        Quadcopter = quadcopter;
        Operator = remoteOperator;
        Accent = accent;
        ToggleRemoteCommand = new RelayCommand(ToggleRemote);
        FailGpsCommand = new RelayCommand(FailGps, () => CanFailGps);
        Refresh();
    }

    public Quadcopter Quadcopter { get; }

    public Operator Operator { get; }

    public IBrush Accent { get; }

    public RelayCommand ToggleRemoteCommand { get; }

    public RelayCommand FailGpsCommand { get; }

    public string Name => Quadcopter.Name;

    public string OperatorName => Operator.Name;

    public double PadLeft => Quadcopter.HomeX - PadRadius;

    public double PadTop => Quadcopter.HomeY - PadRadius;

    public double OperatorLeft => Operator.X - OperatorCenterX;

    public double OperatorTop => Operator.Y - OperatorFeetY;

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

    public double ShadowLeft
    {
        get => _shadowLeft;
        private set => SetProperty(ref _shadowLeft, value);
    }

    public double ShadowTop
    {
        get => _shadowTop;
        private set => SetProperty(ref _shadowTop, value);
    }

    public double ShadowWidth
    {
        get => _shadowWidth;
        private set
        {
            if (SetProperty(ref _shadowWidth, value))
            {
                OnPropertyChanged(nameof(ShadowHeight));
            }
        }
    }

    public double ShadowHeight => ShadowWidth * 0.6;

    public double ShadowOpacity
    {
        get => _shadowOpacity;
        private set => SetProperty(ref _shadowOpacity, value);
    }

    public double RepairBarWidth
    {
        get => _repairBarWidth;
        private set => SetProperty(ref _repairBarWidth, value);
    }

    public string StateText
    {
        get => _stateText;
        private set => SetProperty(ref _stateText, value);
    }

    public string GpsText
    {
        get => _gpsText;
        private set => SetProperty(ref _gpsText, value);
    }

    public string RemoteText
    {
        get => _remoteText;
        private set => SetProperty(ref _remoteText, value);
    }

    public string RemoteButtonText
    {
        get => _remoteButtonText;
        private set => SetProperty(ref _remoteButtonText, value);
    }

    public bool IsSpinning
    {
        get => _isSpinning;
        private set => SetProperty(ref _isSpinning, value);
    }

    public bool IsGpsAlarm
    {
        get => _isGpsAlarm;
        private set => SetProperty(ref _isGpsAlarm, value);
    }

    public bool IsEmergency
    {
        get => _isEmergency;
        private set => SetProperty(ref _isEmergency, value);
    }

    public bool IsRepairing
    {
        get => _isRepairing;
        private set => SetProperty(ref _isRepairing, value);
    }

    public bool IsRemoteOn
    {
        get => _isRemoteOn;
        private set => SetProperty(ref _isRemoteOn, value);
    }

    public bool HasLink
    {
        get => _hasLink;
        private set => SetProperty(ref _hasLink, value);
    }

    public bool CanFailGps
    {
        get => _canFailGps;
        private set
        {
            if (SetProperty(ref _canFailGps, value))
            {
                FailGpsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public Point LinkStart
    {
        get => _linkStart;
        private set => SetProperty(ref _linkStart, value);
    }

    public Point LinkEnd
    {
        get => _linkEnd;
        private set => SetProperty(ref _linkEnd, value);
    }

    public int FlightCount
    {
        get => _flightCount;
        private set => SetProperty(ref _flightCount, value);
    }

    public int GpsFailureCount
    {
        get => _gpsFailureCount;
        private set => SetProperty(ref _gpsFailureCount, value);
    }

    public void Refresh()
    {
        QuadcopterSnapshot snapshot = Quadcopter.GetSnapshot();
        double bodyX = snapshot.X;
        double bodyY = snapshot.Y - snapshot.Altitude;

        Left = bodyX - BodyCenterX;
        Top = bodyY - BodyCenterY;

        ShadowWidth = Math.Max(20, ShadowBaseWidth - snapshot.Altitude * 0.2);
        ShadowLeft = snapshot.X - ShadowWidth / 2;
        ShadowTop = snapshot.Y - ShadowHeight / 2;
        ShadowOpacity = Math.Max(0.12, 0.35 - snapshot.Altitude * 0.002);

        StateText = snapshot.State.GetDescription();
        GpsText = snapshot.IsGpsWorking ? "GPS исправен" : "GPS неисправен";
        IsSpinning = snapshot.State is QuadcopterState.TakingOff
            or QuadcopterState.Flying
            or QuadcopterState.Landing
            or QuadcopterState.EmergencyLanding;
        IsGpsAlarm = !snapshot.IsGpsWorking;
        IsEmergency = snapshot.State == QuadcopterState.EmergencyLanding;
        IsRepairing = snapshot.State == QuadcopterState.Repairing;
        RepairBarWidth = snapshot.RepairProgress * MaxRepairBarWidth;
        CanFailGps = snapshot.State is QuadcopterState.TakingOff
            or QuadcopterState.Flying
            or QuadcopterState.Landing;

        IsRemoteOn = Operator.IsRemoteOn;
        RemoteText = IsRemoteOn ? "пульт включён" : "пульт выключен";
        RemoteButtonText = IsRemoteOn ? "Выключить пульт" : "Включить пульт";
        HasLink = IsRemoteOn && snapshot.IsGpsWorking && IsSpinning;
        LinkStart = new Point(Operator.X + AntennaOffsetX, Operator.Y - AntennaOffsetY);
        LinkEnd = new Point(bodyX, bodyY);

        FlightCount = Quadcopter.FlightCount;
        GpsFailureCount = Quadcopter.GpsFailureCount;
    }

    private void ToggleRemote()
    {
        if (Operator.IsRemoteOn)
        {
            Operator.TurnOffRemote();
        }
        else
        {
            Operator.TurnOnRemote();
        }

        Refresh();
    }

    private void FailGps()
    {
        Quadcopter.FailGps();
        Refresh();
    }
}
