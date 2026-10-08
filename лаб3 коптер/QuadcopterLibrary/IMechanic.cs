namespace QuadcopterLibrary;

public interface IMechanic
{
    event MechanicEventHandler? RepairAttemptFailed;

    string Name { get; }

    string Specialization { get; }

    double X { get; }

    double Y { get; }

    MechanicActivity Activity { get; }

    Quadcopter? Target { get; }

    bool IsBusy { get; }

    int RepairsCompleted { get; }

    bool Repair(Quadcopter quadcopter, CancellationToken token);

    void ReturnToBase(CancellationToken token);
}
