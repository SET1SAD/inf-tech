using System.ComponentModel;

namespace QuadcopterLibrary;

public enum MechanicActivity
{
    [Description("На базе")]
    AtBase,

    [Description("Идёт к квадрокоптеру")]
    GoingToQuadcopter,

    [Description("Ремонтирует")]
    Repairing,

    [Description("Возвращается на базу")]
    Returning
}
