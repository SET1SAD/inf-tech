namespace QuadcopterLibrary;

public class Operator
{
    private readonly RepairService _repairService;
    private volatile bool _isRemoteOn;

    public Operator(string name, double x, double y, Quadcopter quadcopter, RepairService repairService)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(quadcopter);
        ArgumentNullException.ThrowIfNull(repairService);

        Name = name;
        X = x;
        Y = y;
        Quadcopter = quadcopter;
        _repairService = repairService;

        quadcopter.EmergencyLanded += OnEmergencyLanded;
        quadcopter.Repaired += OnRepaired;
        quadcopter.ConnectRemote(this);
    }

    public event RemoteEventHandler? RemoteTurnedOn;

    public event RemoteEventHandler? RemoteTurnedOff;

    public string Name { get; }

    public double X { get; }

    public double Y { get; }

    public Quadcopter Quadcopter { get; }

    public bool IsRemoteOn => _isRemoteOn;

    public void TurnOnRemote()
    {
        if (_isRemoteOn)
        {
            return;
        }

        _isRemoteOn = true;
        RemoteTurnedOn?.Invoke(this);
    }

    public void TurnOffRemote()
    {
        if (!_isRemoteOn)
        {
            return;
        }

        _isRemoteOn = false;
        RemoteTurnedOff?.Invoke(this);
    }

    public override string ToString()
    {
        return $"Оператор {Name}";
    }

    private void OnEmergencyLanded(Quadcopter quadcopter)
    {
        _repairService.RequestRepair(quadcopter);
    }

    private void OnRepaired(Quadcopter quadcopter)
    {
        if (_isRemoteOn)
        {
            quadcopter.TakeOff();
        }
        else
        {
            quadcopter.Land();
        }
    }
}
