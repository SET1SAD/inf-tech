namespace QuadcopterLibrary;

[MechanicInfo("Инженер GPS", "Передвигается медленно, но быстро восстанавливает GPS-модуль")]
public class GpsEngineer : Mechanic
{
    public GpsEngineer(string name, double baseX, double baseY)
        : base(name, baseX, baseY)
    {
    }

    public override double TravelSpeed => 70;

    public override double RepairSeconds => 2;
}
