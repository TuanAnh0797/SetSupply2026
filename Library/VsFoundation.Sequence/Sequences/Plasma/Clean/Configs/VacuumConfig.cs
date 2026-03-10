namespace VsFoundation.Sequence.Sequences.Plasma.Clean.Configs;

/// <summary>
/// Global vacuum configuration parameters used across the application.
/// This defines timing thresholds, sensor fault limits, and operational conditions 
/// for vacuum pump and booster control.
/// </summary>
public sealed class VacuumConfig
{
    /// <summary>
    /// Maximum allowed time (in milliseconds) for the vacuum system 
    /// to reach the target pressure before timing out.
    /// </summary>
    public int VacuumReachTimeoutMs { get; init; } = 120_000;

    /// <summary>
    /// If the measured vacuum pressure is below this Torr threshold, 
    /// it is considered a gauge/sensor fault.
    /// </summary>
    public double GaugeFaultTorrThreshold { get; init; } = 0.002;

    /// <summary>
    /// Booster will only be enabled when the measured pressure is below this Torr value.
    /// </summary>
    public double BoosterEnableBelowTorr { get; init; } = 9.9;

    /// <summary>
    /// Minimum delay (in milliseconds) after turning on the dry pump 
    /// before checking if it is operational.
    /// </summary>
    public int DryPumpMinOnDelayMs { get; init; } = 2_000;

    public int CeidVacuumStart { get; init; }

    public int CeidVacuumCompleted { get; init; }

    public int AlarmPumpOilCheckPm1 { get; init; }

    public int AlarmChamberClose { get; init; }

    public int AlarmVacuumTimeOver { get; init; }

    public int AlarmVacGaugeFault { get; init; }
}

