using System.ComponentModel;

namespace QuadcopterLibrary;

public enum QuadcopterState
{
    [Description("На площадке")]
    Idle,

    [Description("Взлёт")]
    TakingOff,

    [Description("Полёт")]
    Flying,

    [Description("Возврат и посадка")]
    Landing,

    [Description("Аварийная посадка")]
    EmergencyLanding,

    [Description("Ожидает механика")]
    WaitingForRepair,

    [Description("Ремонт")]
    Repairing
}
