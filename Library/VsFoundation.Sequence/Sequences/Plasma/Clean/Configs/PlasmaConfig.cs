using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Sequence.Sequences.Plasma.Clean.Configs;

public sealed class PlasmaConfig
{
    public int GasCheckRfOnDelayMs { get; init; } = 5_000;

    public int RfPowerOnCheckDelayMs { get; init; } = 10_000;

    public int RfStableTimeMs { get; init; } = 3_000;

    public int SoftTickMs { get; init; } = 200;
    public int SoftStart { get; init; } = 0;
    public int SoftEnd { get; init; } = 0;

    // RF/Gas error monitor
    public int RfErrorHoldMs { get; init; } = 100;
    public int GasErrorGuardSec { get; init; } = 10;

    public int CeidPlasmaStarted { get; init; }
    public int CeidPlasmaCompleted { get; init; }

    public int AlarmRfOnTimeout { get; init; }
    public int AlarmRfOnError { get; init; }
    public int AlarmOverPressure { get; init; }
    public int AlarmVacGaugeFault { get; init; }
}
