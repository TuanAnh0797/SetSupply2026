namespace VsFoundation.Sequence.Sequences.Plasma.Clean.Configs;

/// <summary>
/// Global configuration parameters for the gas purging process.
/// Defines timing constraints, stabilization delays, and error handling 
/// thresholds used during the purging sequence.
/// </summary>
public sealed class GasPurgingConfig
{
    /// <summary>
    /// Maximum time (in milliseconds) to wait for the gas level 
    /// to reach "OK" status before treating it as an error.
    /// </summary>
    public int GasErrorTimeMs { get; init; } = 10_000;

    /// <summary>
    /// Delay (in milliseconds) after gas level becomes OK 
    /// to allow stabilization before continuing.
    /// </summary>
    public int GasOnCheckDelayMs { get; init; } = 5_000;

    /// <summary>
    /// Maximum time (in milliseconds) to wait for vacuum pressure 
    /// to reach the stable target before reporting an error.
    /// </summary>
    public int VacReachingTimeMs { get; init; } = 120_000;

    /// <summary>
    /// Additional stabilization time (in milliseconds) to maintain 
    /// "Vacuum Stable" before ending the process.
    /// </summary>
    public int VacStableTimeMs { get; init; } = 3_000;

    /// <summary>
    /// Pulse interval (in milliseconds) for logging gas error events. 
    /// Default matches legacy code at 500 ms.
    /// </summary>
    public int GasErrorPulseMs { get; init; } = 500;

    public int AlarmVacuumTimeOver { get; init; }
}
