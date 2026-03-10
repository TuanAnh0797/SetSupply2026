namespace VsFoundation.Sequence.Sequences.Plasma.Clean.Configs;

public class LeakCheckConfig
{
    public int OverPumpingTimeMs { get; init; } = 30_000;
    public int StableTimeMs { get; init; } = 10_000;
    public int LeakCheckTimeMs { get; init; } = 60_000;
    public double LeakAlarmRateTorr { get; init; } = 0.2;
    public int VentilationTimeMs { get; init; } = 10_000;
}
