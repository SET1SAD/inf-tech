using System.Collections.Concurrent;

namespace QuadcopterLibrary;

public sealed class RepairService : IDisposable
{
    private const int WaitMilliseconds = 100;

    private readonly BlockingCollection<Quadcopter> _requests = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly List<IMechanic> _mechanics = new();
    private readonly List<Thread> _workers = new();
    private readonly object _sync = new();

    public event QuadcopterEventHandler? RepairRequested;

    public event MechanicEventHandler? RepairStarted;

    public event MechanicEventHandler? RepairCompleted;

    public int PendingCount => _requests.Count;

    public IReadOnlyList<IMechanic> Mechanics
    {
        get
        {
            lock (_sync)
            {
                return _mechanics.ToArray();
            }
        }
    }

    public void Hire(IMechanic mechanic)
    {
        ArgumentNullException.ThrowIfNull(mechanic);

        Thread worker = new(() => Work(mechanic))
        {
            IsBackground = true,
            Name = $"Механик {mechanic.Name}"
        };

        lock (_sync)
        {
            _mechanics.Add(mechanic);
            _workers.Add(worker);
        }

        worker.Start();
    }

    public void RequestRepair(Quadcopter quadcopter)
    {
        ArgumentNullException.ThrowIfNull(quadcopter);

        if (_cancellation.IsCancellationRequested)
        {
            return;
        }

        _requests.Add(quadcopter);
        RepairRequested?.Invoke(quadcopter);
    }

    public void Dispose()
    {
        _cancellation.Cancel();

        Thread[] workers;
        lock (_sync)
        {
            workers = _workers.ToArray();
        }

        foreach (Thread worker in workers)
        {
            worker.Join();
        }

        _requests.Dispose();
        _cancellation.Dispose();
    }

    private void Work(IMechanic mechanic)
    {
        CancellationToken token = _cancellation.Token;

        while (!token.IsCancellationRequested)
        {
            if (_requests.TryTake(out Quadcopter? quadcopter, WaitMilliseconds))
            {
                Serve(mechanic, quadcopter, token);
            }
        }
    }

    private void Serve(IMechanic mechanic, Quadcopter quadcopter, CancellationToken token)
    {
        RepairStarted?.Invoke(mechanic, quadcopter);

        if (mechanic.Repair(quadcopter, token))
        {
            RepairCompleted?.Invoke(mechanic, quadcopter);
        }
        else if (!token.IsCancellationRequested && quadcopter.State == QuadcopterState.WaitingForRepair)
        {
            _requests.Add(quadcopter);
        }

        if (!token.IsCancellationRequested && _requests.Count == 0)
        {
            mechanic.ReturnToBase(token);
        }
    }
}
