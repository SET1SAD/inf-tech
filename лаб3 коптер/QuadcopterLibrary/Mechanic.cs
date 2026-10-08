using System.Diagnostics;

namespace QuadcopterLibrary;

public abstract class Mechanic : IMechanic
{
    private const int StepMilliseconds = 20;
    private const double WorkOffsetX = 50;
    private const double WorkOffsetY = 6;

    private readonly object _sync = new();
    private double _x;
    private double _y;
    private MechanicActivity _activity;
    private Quadcopter? _target;
    private int _repairsCompleted;

    protected Mechanic(string name, double baseX, double baseY)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        BaseX = baseX;
        BaseY = baseY;
        _x = baseX;
        _y = baseY;
    }

    public event MechanicEventHandler? RepairAttemptFailed;

    public string Name { get; }

    public double BaseX { get; }

    public double BaseY { get; }

    public string Specialization => MechanicCatalog.GetInfo(GetType()).Title;

    public double X => ReadLocked(() => _x);

    public double Y => ReadLocked(() => _y);

    public MechanicActivity Activity => ReadLocked(() => _activity);

    public Quadcopter? Target => ReadLocked(() => _target);

    public bool IsBusy => Activity != MechanicActivity.AtBase;

    public int RepairsCompleted => Volatile.Read(ref _repairsCompleted);

    public abstract double TravelSpeed { get; }

    public abstract double RepairSeconds { get; }

    public bool Repair(Quadcopter quadcopter, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(quadcopter);

        SetActivity(MechanicActivity.GoingToQuadcopter, quadcopter);
        QuadcopterSnapshot place = quadcopter.GetSnapshot();
        if (!WalkTo(place.X - WorkOffsetX, place.Y + WorkOffsetY, token) || !quadcopter.BeginRepair())
        {
            return false;
        }

        SetActivity(MechanicActivity.Repairing, quadcopter);
        Stopwatch clock = Stopwatch.StartNew();
        while (clock.Elapsed.TotalSeconds < RepairSeconds)
        {
            if (token.WaitHandle.WaitOne(StepMilliseconds))
            {
                quadcopter.AbortRepair();
                return false;
            }

            quadcopter.ReportRepairProgress(clock.Elapsed.TotalSeconds / RepairSeconds);
        }

        if (!IsRepairSuccessful())
        {
            quadcopter.AbortRepair();
            RepairAttemptFailed?.Invoke(this, quadcopter);
            return false;
        }

        quadcopter.CompleteRepair();
        Interlocked.Increment(ref _repairsCompleted);
        return true;
    }

    public void ReturnToBase(CancellationToken token)
    {
        SetActivity(MechanicActivity.Returning, null);
        if (WalkTo(BaseX, BaseY, token))
        {
            SetActivity(MechanicActivity.AtBase, null);
        }
    }

    public override string ToString()
    {
        return $"{Specialization} {Name}";
    }

    protected virtual bool IsRepairSuccessful()
    {
        return true;
    }

    private T ReadLocked<T>(Func<T> read)
    {
        lock (_sync)
        {
            return read();
        }
    }

    private void SetActivity(MechanicActivity activity, Quadcopter? target)
    {
        lock (_sync)
        {
            _activity = activity;
            _target = target;
        }
    }

    private bool WalkTo(double targetX, double targetY, CancellationToken token)
    {
        Stopwatch clock = Stopwatch.StartNew();
        TimeSpan previous = TimeSpan.Zero;

        while (true)
        {
            TimeSpan now = clock.Elapsed;
            double step = TravelSpeed * (now - previous).TotalSeconds;
            previous = now;

            if (StepTowards(targetX, targetY, step))
            {
                return true;
            }

            if (token.WaitHandle.WaitOne(StepMilliseconds))
            {
                return false;
            }
        }
    }

    private bool StepTowards(double targetX, double targetY, double step)
    {
        lock (_sync)
        {
            double dx = targetX - _x;
            double dy = targetY - _y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance <= step)
            {
                _x = targetX;
                _y = targetY;
                return true;
            }

            _x += dx / distance * step;
            _y += dy / distance * step;
            return false;
        }
    }
}
