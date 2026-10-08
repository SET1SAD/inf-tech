namespace QuadcopterLibrary;

public readonly record struct FlightArea(double Left, double Top, double Right, double Bottom)
{
    public bool Contains(double x, double y)
    {
        return x >= Left && x <= Right && y >= Top && y <= Bottom;
    }

    public (double X, double Y) GetRandomPoint()
    {
        double x = Left + Random.Shared.NextDouble() * (Right - Left);
        double y = Top + Random.Shared.NextDouble() * (Bottom - Top);
        return (x, y);
    }
}
