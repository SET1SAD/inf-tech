namespace QuadcopterLibrary;

public readonly record struct QuadcopterSnapshot(
    double X,
    double Y,
    double Altitude,
    QuadcopterState State,
    bool IsGpsWorking,
    double RepairProgress);
