using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia.Media;
using Avalonia.Threading;
using QuadcopterLibrary;

namespace QuadcopterApp.ViewModels;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const int FrameMilliseconds = 33;
    private const int InspectorRefreshFrames = 8;
    private const int MaxQuadcopters = 14;
    private const int MaxMechanics = 12;
    private const int PadsPerRow = 7;
    private const int MechanicsPerRow = 6;
    private const int MaxLogLength = 200;

    private static readonly string[] QuadcopterColors =
    {
        "#2F6FBF", "#D35400", "#1E8449", "#8E44AD", "#C0392B", "#117A65", "#B7950B"
    };

    private static readonly string[] MechanicColors = { "#E67E22", "#2980B9", "#27AE60", "#7F8C8D" };

    private static readonly string[] OperatorNames =
    {
        "Иван", "Мария", "Олег", "Анна", "Павел", "Елена", "Сергей",
        "Ольга", "Дмитрий", "Наталья", "Артём", "Ксения", "Максим", "Юлия"
    };

    private static readonly string[] MechanicNames =
    {
        "Петров", "Сидоров", "Кузнецов", "Смирнов", "Попов", "Васильев",
        "Новиков", "Фёдоров", "Морозов", "Волков", "Алексеев", "Лебедев"
    };

    private readonly RepairService _repairService;
    private readonly FlightArea _flightArea;
    private readonly DispatcherTimer _timer;
    private int _frame;
    private double _gpsFailurePercent;
    private double _flightSpeed;
    private MechanicTypeOption _selectedMechanicType;
    private InspectableObject? _selectedInspectable;
    private string _inspectedTypeText = string.Empty;
    private string _inspectedInterfacesText = string.Empty;
    private string _inspectedEventsText = string.Empty;
    private string _inspectedMethodsText = string.Empty;
    private int _airborneCount;
    private int _totalFlights;
    private int _totalGpsFailures;
    private int _totalRepairs;
    private int _pendingRepairs;

    public MainWindowViewModel(RepairService repairService, FlightArea flightArea, double gpsFailurePercent, double flightSpeed)
    {
        _repairService = repairService;
        _flightArea = flightArea;
        _gpsFailurePercent = gpsFailurePercent;
        _flightSpeed = flightSpeed;

        MechanicTypes = MechanicCatalog.FindMechanicTypes()
            .Select(CreateMechanicTypeOption)
            .ToList();
        _selectedMechanicType = MechanicTypes[0];

        _repairService.RepairRequested += OnRepairRequested;
        _repairService.RepairStarted += OnRepairStarted;
        _repairService.RepairCompleted += OnRepairCompleted;

        AddQuadcopterCommand = new RelayCommand(AddQuadcopter, () => Quadcopters.Count < MaxQuadcopters);
        HireMechanicCommand = new RelayCommand(HireSelectedMechanic, () => Mechanics.Count < MaxMechanics);
        AllRemotesOnCommand = new RelayCommand(TurnOnAllRemotes);
        AllRemotesOffCommand = new RelayCommand(TurnOffAllRemotes);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(FrameMilliseconds) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    public ObservableCollection<QuadcopterViewModel> Quadcopters { get; } = new();

    public ObservableCollection<MechanicViewModel> Mechanics { get; } = new();

    public ObservableCollection<InspectableObject> Inspectables { get; } = new();

    public ObservableCollection<PropertyRowViewModel> InspectorRows { get; } = new();

    public ObservableCollection<string> EventLog { get; } = new();

    public IReadOnlyList<MechanicTypeOption> MechanicTypes { get; }

    public RelayCommand AddQuadcopterCommand { get; }

    public RelayCommand HireMechanicCommand { get; }

    public RelayCommand AllRemotesOnCommand { get; }

    public RelayCommand AllRemotesOffCommand { get; }

    public double GpsFailurePercent
    {
        get => _gpsFailurePercent;
        set
        {
            if (SetProperty(ref _gpsFailurePercent, value))
            {
                foreach (QuadcopterViewModel item in Quadcopters)
                {
                    item.Quadcopter.GpsFailureProbability = value / 100;
                }
            }
        }
    }

    public double FlightSpeed
    {
        get => _flightSpeed;
        set
        {
            if (SetProperty(ref _flightSpeed, value))
            {
                foreach (QuadcopterViewModel item in Quadcopters)
                {
                    item.Quadcopter.Speed = value;
                }
            }
        }
    }

    public MechanicTypeOption SelectedMechanicType
    {
        get => _selectedMechanicType;
        set => SetProperty(ref _selectedMechanicType, value);
    }

    public InspectableObject? SelectedInspectable
    {
        get => _selectedInspectable;
        set
        {
            if (SetProperty(ref _selectedInspectable, value))
            {
                InspectorRows.Clear();
                RefreshInspector();
            }
        }
    }

    public string InspectedTypeText
    {
        get => _inspectedTypeText;
        private set => SetProperty(ref _inspectedTypeText, value);
    }

    public string InspectedInterfacesText
    {
        get => _inspectedInterfacesText;
        private set => SetProperty(ref _inspectedInterfacesText, value);
    }

    public string InspectedEventsText
    {
        get => _inspectedEventsText;
        private set => SetProperty(ref _inspectedEventsText, value);
    }

    public string InspectedMethodsText
    {
        get => _inspectedMethodsText;
        private set => SetProperty(ref _inspectedMethodsText, value);
    }

    public int AirborneCount
    {
        get => _airborneCount;
        private set => SetProperty(ref _airborneCount, value);
    }

    public int TotalFlights
    {
        get => _totalFlights;
        private set => SetProperty(ref _totalFlights, value);
    }

    public int TotalGpsFailures
    {
        get => _totalGpsFailures;
        private set => SetProperty(ref _totalGpsFailures, value);
    }

    public int TotalRepairs
    {
        get => _totalRepairs;
        private set => SetProperty(ref _totalRepairs, value);
    }

    public int PendingRepairs
    {
        get => _pendingRepairs;
        private set => SetProperty(ref _pendingRepairs, value);
    }

    public void AddQuadcopter()
    {
        int index = Quadcopters.Count;
        if (index >= MaxQuadcopters)
        {
            return;
        }

        double homeX = 90 + index % PadsPerRow * 120;
        double homeY = 545 - index / PadsPerRow * 95;
        double cruiseAltitude = 60 + Random.Shared.Next(0, 4) * 10;

        Quadcopter quadcopter = new(
            $"Дрон-{index + 1}",
            homeX,
            homeY,
            cruiseAltitude,
            FlightSpeed,
            GpsFailurePercent / 100,
            _flightArea);
        Operator remoteOperator = new(
            OperatorNames[index % OperatorNames.Length],
            homeX - 46,
            homeY + 22,
            quadcopter,
            _repairService);

        quadcopter.StateChanged += OnStateChanged;
        quadcopter.GpsLost += OnGpsLost;
        quadcopter.EmergencyLanded += OnEmergencyLanded;
        remoteOperator.RemoteTurnedOn += OnRemoteTurnedOn;
        remoteOperator.RemoteTurnedOff += OnRemoteTurnedOff;

        IBrush accent = Brush.Parse(QuadcopterColors[index % QuadcopterColors.Length]);
        Quadcopters.Add(new QuadcopterViewModel(quadcopter, remoteOperator, accent));
        Inspectables.Add(new InspectableObject(quadcopter.ToString(), quadcopter));
        Inspectables.Add(new InspectableObject(remoteOperator.ToString(), remoteOperator));
        SelectedInspectable ??= Inspectables[0];

        quadcopter.Start();
        AddLog($"Добавлен {quadcopter}, оператор {remoteOperator.Name}");
        AddQuadcopterCommand.RaiseCanExecuteChanged();
    }

    public void HireMechanic(MechanicTypeOption option)
    {
        int index = Mechanics.Count;
        if (index >= MaxMechanics)
        {
            return;
        }

        double baseX = 52 + index % MechanicsPerRow * 34;
        double baseY = 92 + index / MechanicsPerRow * 52;
        IMechanic mechanic = MechanicCatalog.Create(option.Type, MechanicNames[index % MechanicNames.Length], baseX, baseY);

        mechanic.RepairAttemptFailed += OnRepairAttemptFailed;
        _repairService.Hire(mechanic);

        Mechanics.Add(new MechanicViewModel(mechanic, option.Accent));
        Inspectables.Add(new InspectableObject(mechanic.ToString() ?? mechanic.Name, mechanic));
        AddLog($"Нанят механик: {mechanic.Specialization} {mechanic.Name}");
        HireMechanicCommand.RaiseCanExecuteChanged();
    }

    public void Dispose()
    {
        _timer.Stop();

        foreach (QuadcopterViewModel item in Quadcopters)
        {
            item.Quadcopter.Dispose();
        }

        _repairService.Dispose();
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "—",
            double number => number.ToString("0.##"),
            bool flag => flag ? "да" : "нет",
            Enum enumValue => enumValue.GetDescription(),
            _ => value.ToString() ?? string.Empty
        };
    }

    private MechanicTypeOption CreateMechanicTypeOption(Type type, int index)
    {
        MechanicInfoAttribute info = MechanicCatalog.GetInfo(type);
        IBrush accent = Brush.Parse(MechanicColors[index % MechanicColors.Length]);
        return new MechanicTypeOption(type, info.Title, info.Description, accent);
    }

    private void HireSelectedMechanic()
    {
        HireMechanic(SelectedMechanicType);
    }

    private void TurnOnAllRemotes()
    {
        foreach (QuadcopterViewModel item in Quadcopters)
        {
            item.Operator.TurnOnRemote();
        }
    }

    private void TurnOffAllRemotes()
    {
        foreach (QuadcopterViewModel item in Quadcopters)
        {
            item.Operator.TurnOffRemote();
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        foreach (QuadcopterViewModel item in Quadcopters)
        {
            item.Refresh();
        }

        foreach (MechanicViewModel item in Mechanics)
        {
            item.Refresh();
        }

        AirborneCount = Quadcopters.Count(item => item.IsSpinning);
        TotalFlights = Quadcopters.Sum(item => item.FlightCount);
        TotalGpsFailures = Quadcopters.Sum(item => item.GpsFailureCount);
        TotalRepairs = Mechanics.Sum(item => item.RepairsCompleted);
        PendingRepairs = _repairService.PendingCount;

        _frame++;
        if (_frame % InspectorRefreshFrames == 0)
        {
            RefreshInspector();
        }
    }

    private void RefreshInspector()
    {
        object? target = SelectedInspectable?.Target;
        if (target is null)
        {
            InspectorRows.Clear();
            InspectedTypeText = string.Empty;
            InspectedInterfacesText = string.Empty;
            InspectedEventsText = string.Empty;
            InspectedMethodsText = string.Empty;
            return;
        }

        Type type = target.GetType();
        InspectedTypeText = type.BaseType is null || type.BaseType == typeof(object)
            ? type.Name
            : $"{type.Name} : {type.BaseType.Name}";

        Type[] interfaces = type.GetInterfaces();
        InspectedInterfacesText = interfaces.Length == 0
            ? "нет"
            : string.Join(", ", interfaces.Select(item => item.Name));

        InspectedEventsText = string.Join(", ", type.GetEvents().Select(item => item.Name));

        InspectedMethodsText = string.Join(", ", type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => !method.IsSpecialName && method.DeclaringType != typeof(object))
            .Select(method => method.Name)
            .Distinct());

        PropertyInfo[] properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0)
            .ToArray();

        if (InspectorRows.Count != properties.Length)
        {
            InspectorRows.Clear();
            foreach (PropertyInfo property in properties)
            {
                InspectorRows.Add(new PropertyRowViewModel(property.Name, property.PropertyType.Name, string.Empty));
            }
        }

        for (int i = 0; i < properties.Length; i++)
        {
            InspectorRows[i].Value = FormatValue(properties[i].GetValue(target));
        }
    }

    private void OnStateChanged(Quadcopter quadcopter, QuadcopterState oldState, QuadcopterState newState)
    {
        string? message = newState switch
        {
            QuadcopterState.TakingOff => $"{quadcopter.Name}: взлёт",
            QuadcopterState.Landing => $"{quadcopter.Name}: возврат на площадку",
            QuadcopterState.Idle when oldState == QuadcopterState.Landing => $"{quadcopter.Name}: посадка на площадку выполнена",
            QuadcopterState.Repairing => $"{quadcopter.Name}: начат ремонт",
            _ => null
        };

        if (message is not null)
        {
            PostLog(message);
        }
    }

    private void OnGpsLost(Quadcopter quadcopter)
    {
        PostLog($"{quadcopter.Name}: СБОЙ GPS, аварийная посадка");
    }

    private void OnEmergencyLanded(Quadcopter quadcopter)
    {
        PostLog($"{quadcopter.Name}: аварийная посадка выполнена, вызов механика");
    }

    private void OnRemoteTurnedOn(Operator remoteOperator)
    {
        PostLog($"Оператор {remoteOperator.Name}: пульт {remoteOperator.Quadcopter.Name} включён");
    }

    private void OnRemoteTurnedOff(Operator remoteOperator)
    {
        PostLog($"Оператор {remoteOperator.Name}: пульт {remoteOperator.Quadcopter.Name} выключен");
    }

    private void OnRepairRequested(Quadcopter quadcopter)
    {
        PostLog($"Заявка на ремонт {quadcopter.Name}, в очереди: {_repairService.PendingCount}");
    }

    private void OnRepairStarted(IMechanic mechanic, Quadcopter quadcopter)
    {
        PostLog($"{mechanic.Specialization} {mechanic.Name} принял заявку {quadcopter.Name}");
    }

    private void OnRepairCompleted(IMechanic mechanic, Quadcopter quadcopter)
    {
        PostLog($"{quadcopter.Name} отремонтирован ({mechanic.Specialization} {mechanic.Name})");
    }

    private void OnRepairAttemptFailed(IMechanic mechanic, Quadcopter quadcopter)
    {
        PostLog($"{quadcopter.Name}: ремонт не удался ({mechanic.Specialization} {mechanic.Name}), заявка повторена");
    }

    private void PostLog(string message)
    {
        string record = $"{DateTime.Now:HH:mm:ss}  {message}";
        Dispatcher.UIThread.Post(() => InsertLog(record));
    }

    private void AddLog(string message)
    {
        InsertLog($"{DateTime.Now:HH:mm:ss}  {message}");
    }

    private void InsertLog(string record)
    {
        EventLog.Insert(0, record);
        if (EventLog.Count > MaxLogLength)
        {
            EventLog.RemoveAt(EventLog.Count - 1);
        }
    }
}
