namespace QuadcopterLibrary;

[MechanicInfo("Выездной механик", "Быстро добирается до места аварии, но чинит дольше остальных")]
public class FieldMechanic : Mechanic
{
    public FieldMechanic(string name, double baseX, double baseY)
        : base(name, baseX, baseY)
    {
    }

    public override double TravelSpeed => 130;

    public override double RepairSeconds => 4;
}
