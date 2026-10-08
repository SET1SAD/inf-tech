using System.Diagnostics;

namespace QuadcopterLibrary;

public sealed class Quadcopter : IDisposable
{
    private const int TickMilliseconds = 20;
    private const double ClimbRate = 40;
    private const double DescentRate = 32;
    private const double EmergencyDescentRate = 16;
    private const double DriftSpeed = 28;
    private const double HomeTolerance = 0.5;

    private readonly object _sync = new();
    private readonly FlightArea _area;
    private Thread? _thread;
    private volatile bool _isRunning;
    private Operator? _remoteOperator;
    private double _speed;
    private double _gpsFailureProbability;
    private double _x;
    private double _y;
    private double _altitude;
    private double _targetX;
    private double _targetY;
    private double _driftX;
    private double _driftY;
    private double _repairProgress;
    private QuadcopterState _state;
    private bool _isGpsFailed;
    private int _gpsFailureCount;
    private int _flightCount;

    public Quadcopter(
        string name,
        double homeX,
        double homeY,
        double cruiseAltitude,
        double speed,
        double gpsFailureProbability,
        FlightArea area)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cruiseAltitude);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(speed);

        Name = name;
        HomeX = homeX;
        HomeY = homeY;
        CruiseAltitude = cruiseAltitude;
        _speed = speed;
        _gpsFailureProbability = Math.Clamp(gpsFailureProbability, 0, 1);
        _area = area;
        _x = homeX;
        _y = homeY;
    }

    public event StateChangedEventHandler? StateChanged;

    public event QuadcopterEventHandler? GpsLost;

    public event QuadcopterEventHandler? EmergencyLanded;

    public event QuadcopterEventHandler? Repaired;

    public string Name { get; }

    public double HomeX { get; }

    public double HomeY { get; }

    public double CruiseAltitude { get; }

    public Operator? RemoteOperator => _remoteOperator;

    public bool IsRunning => _isRunning;

    public double Speed
    {
        get => ReadLocked(() => _speed);
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            lock (_sync)
            {
                _speed = value;
            }
        }
    }

    public double GpsFailureProbability
    {
        get => ReadLocked(() => _gpsFailureProbability);
        set
        {
            lock (_sync)
            {
                _gpsFailureProbability = Math.Clamp(value, 0, 1);
            }
        }
    }

    public double X => ReadLocked(() => _x);

    public double Y => ReadLocked(() => _y);

    public double Altitude => ReadLocked(() => _altitude);

    public QuadcopterState State => ReadLocked(() => _state);

    public bool IsGpsWorking => ReadLocked(() => !_isGpsFailed);

    public bool IsAirborne => ReadLocked(() => _altitude > 0);

    public bool IsAtHome => ReadLocked(IsAtHomeCore);

    public double RepairProgress => ReadLocked(() => _repairProgress);

    public int GpsFailureCount => ReadLocked(() => _gpsFailureCount);

    public int FlightCount => ReadLocked(() => _flightCount);

    public QuadcopterSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return new QuadcopterSnapshot(_x, _y, _altitude, _state, !_isGpsFailed, _repairProgress);
        }
    }

    public void Start()
    {
        if (_thread is not null)
        {
            return;
        }

        _isRunning = true;
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = $"Квадрокоптер {Name}"
        };
        _thread.Start();
    }

    public void Dispose()
    {
        _isRunning = false;
        _thread?.Join();
        _thread = null;
        DisconnectRemote();
    }

    public void ConnectRemote(Operator remoteOperator)
    {
        ArgumentNullException.ThrowIfNull(remoteOperator);

        DisconnectRemote();
        remoteOperator.RemoteTurnedOn += OnRemoteTurnedOn;
        remoteOperator.RemoteTurnedOff += OnRemoteTurnedOff;
        _remoteOperator = remoteOperator;
    }

    public void DisconnectRemote()
    {
        if (_remoteOperator is null)
        {
            return;
        }

        _remoteOperator.RemoteTurnedOn -= OnRemoteTurnedOn;
        _remoteOperator.RemoteTurnedOff -= OnRemoteTurnedOff;
        _remoteOperator = null;
    }

    public bool TakeOff()
    {
        Action notification;
        lock (_sync)
        {
            bool canTakeOff = !_isGpsFailed
                && (_state == QuadcopterState.Idle || _state == QuadcopterState.Landing);
            if (!canTakeOff)
            {
                return false;
            }

            if (_state == QuadcopterState.Idle)
            {
                _flightCount++;
            }

            notification = SetState(QuadcopterState.TakingOff);
        }

        notification();
        return true;
    }

    public bool Land()
    {
        Action notification;
        lock (_sync)
        {
            bool canLand = _state == QuadcopterState.TakingOff
                || _state == QuadcopterState.Flying
                || (_state == QuadcopterState.Idle && !IsAtHomeCore());
            if (!canLand)
            {
                return false;
            }

            notification = SetState(QuadcopterState.Landing);
        }

        notification();
        return true;
    }

    public bool FailGps()
    {
        Action notifications;
        lock (_sync)
        {
            if (!IsControlledFlight(_state))
            {
                return false;
            }

            notifications = FailGpsCore();
        }

        notifications();
        return true;
    }

    public bool BeginRepair()
    {
        Action notification;
        lock (_sync)
        {
            if (_state != QuadcopterState.WaitingForRepair)
            {
                return false;
            }

            _repairProgress = 0;
            notification = SetState(QuadcopterState.Repairing);
        }

        notification();
        return true;
    }

    public void ReportRepairProgress(double progress)
    {
        lock (_sync)
        {
            if (_state == QuadcopterState.Repairing)
            {
                _repairProgress = Math.Clamp(progress, 0, 1);
            }
        }
    }

    public bool CompleteRepair()
    {
        Action notifications;
        lock (_sync)
        {
            if (_state != QuadcopterState.Repairing)
            {
                return false;
            }

            _isGpsFailed = false;
            _repairProgress = 0;
            notifications = SetState(QuadcopterState.Idle);
            notifications += () => Repaired?.Invoke(this);
        }

        notifications();
        return true;
    }

    public bool AbortRepair()
    {
        Action notification;
        lock (_sync)
        {
            if (_state != QuadcopterState.Repairing)
            {
                return false;
            }

            _repairProgress = 0;
            notification = SetState(QuadcopterState.WaitingForRepair);
        }

        notification();
        return true;
    }

    public override string ToString()
    {
        return $"Квадрокоптер {Name}";
    }

    private static bool IsControlledFlight(QuadcopterState state)
    {
        return state == QuadcopterState.TakingOff
            || state == QuadcopterState.Flying
            || state == QuadcopterState.Landing;
    }

    private T ReadLocked<T>(Func<T> read)
    {
        lock (_sync)
        {
            return read();
        }
    }

    private void OnRemoteTurnedOn(Operator remoteOperator)
    {
        TakeOff();
    }

    private void OnRemoteTurnedOff(Operator remoteOperator)
    {
        Land();
    }

    private void Run()
    {
        Stopwatch clock = Stopwatch.StartNew();
        TimeSpan previous = TimeSpan.Zero;

        while (_isRunning)
        {
            TimeSpan now = clock.Elapsed;
            Update((now - previous).TotalSeconds);
            previous = now;
            Thread.Sleep(TickMilliseconds);
        }
    }

    private void Update(double seconds)
    {
        Action? notifications = null;

        lock (_sync)
        {
            if (IsControlledFlight(_state) && IsGpsFailureHappened(seconds))
            {
                notifications += FailGpsCore();
            }
            else
            {
                notifications += _state switch
                {
                    QuadcopterState.TakingOff => UpdateTakeOff(seconds),
                    QuadcopterState.Flying => UpdateFlight(seconds),
                    QuadcopterState.Landing => UpdateLanding(seconds),
                    QuadcopterState.EmergencyLanding => UpdateEmergencyLanding(seconds),
                    _ => null
                };
            }
        }

        notifications?.Invoke();
    }

    private Action? UpdateTakeOff(double seconds)
    {
        _altitude = Math.Min(CruiseAltitude, _altitude + ClimbRate * seconds);
        if (_altitude < CruiseAltitude)
        {
            return null;
        }

        ChooseWaypoint();
        return SetState(QuadcopterState.Flying);
    }

    private Action? UpdateFlight(double seconds)
    {
        if (MoveTowards(_targetX, _targetY, _speed * seconds))
        {
            ChooseWaypoint();
        }

        return null;
    }

    private Action? UpdateLanding(double seconds)
    {
        if (!MoveTowards(HomeX, HomeY, _speed * seconds))
        {
            _altitude = Math.Min(CruiseAltitude, _altitude + ClimbRate * seconds);
            return null;
        }

        _altitude = Math.Max(0, _altitude - DescentRate * seconds);
        return _altitude > 0 ? null : SetState(QuadcopterState.Idle);
    }

    private Action? UpdateEmergencyLanding(double seconds)
    {
        double nextX = _x + _driftX * seconds;
        double nextY = _y + _driftY * seconds;
        if (_area.Contains(nextX, nextY))
        {
            _x = nextX;
            _y = nextY;
        }

        _altitude = Math.Max(0, _altitude - EmergencyDescentRate * seconds);
        if (_altitude > 0)
        {
            return null;
        }

        Action notifications = SetState(QuadcopterState.WaitingForRepair);
        notifications += () => EmergencyLanded?.Invoke(this);
        return notifications;
    }

    private bool IsGpsFailureHappened(double seconds)
    {
        if (_gpsFailureProbability <= 0)
        {
            return false;
        }

        double probability = 1 - Math.Pow(1 - _gpsFailureProbability, seconds);
        return Random.Shared.NextDouble() < probability;
    }

    private Action FailGpsCore()
    {
        _isGpsFailed = true;
        _gpsFailureCount++;

        double angle = Random.Shared.NextDouble() * 2 * Math.PI;
        _driftX = Math.Cos(angle) * DriftSpeed;
        _driftY = Math.Sin(angle) * DriftSpeed;

        Action notifications = SetState(QuadcopterState.EmergencyLanding);
        notifications += () => GpsLost?.Invoke(this);
        return notifications;
    }

    private Action SetState(QuadcopterState newState)
    {
        QuadcopterState oldState = _state;
        _state = newState;
        return () => StateChanged?.Invoke(this, oldState, newState);
    }

    private void ChooseWaypoint()
    {
        (_targetX, _targetY) = _area.GetRandomPoint();
    }

    private bool MoveTowards(double targetX, double targetY, double distance)
    {
        double dx = targetX - _x;
        double dy = targetY - _y;
        double length = Math.Sqrt(dx * dx + dy * dy);

        if (length <= distance)
        {
            _x = targetX;
            _y = targetY;
            return true;
        }

        _x += dx / length * distance;
        _y += dy / length * distance;
        return false;
    }

    private bool IsAtHomeCore()
    {
        return Math.Abs(_x - HomeX) < HomeTolerance && Math.Abs(_y - HomeY) < HomeTolerance;
    }
}
