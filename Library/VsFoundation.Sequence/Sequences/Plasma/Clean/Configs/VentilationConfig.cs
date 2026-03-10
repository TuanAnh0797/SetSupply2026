using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Sequence.Sequences.Plasma.Clean.Configs;

public sealed class VentilationConfig
{
    public int AutoDoorTimeoutMs { get; init; } = 24_000;
    // CEID mapping
    public int CeidVacPurgeStarted { get; init; }
    public int CeidProcessEnd { get; init; }
    // Alarm mapping
    public int AlarmCannotPurge { get; init; }
    public int AlarmVacNotPurged { get; init; }
}
