namespace QuadcopterLibrary;

[MechanicInfo("Стажёр", "Иногда ремонт не удаётся, и квадрокоптер ждёт повторного ремонта")]
public class TraineeMechanic : Mechanic
{
    private const double FailureChance = 0.4;

    public TraineeMechanic(string name, double baseX, double baseY)
        : base(name, baseX, baseY)
    {
    }

    public override double TravelSpeed => 95;

    public override double RepairSeconds => 3;

    protected override bool IsRepairSuccessful()
    {
        return Random.Shared.NextDouble() >= FailureChance;
    }
}
