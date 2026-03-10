namespace VsFoundation.Sequence.Sequences.Plasma.Clean.Configs;

public interface IRecipeProvider
{
    int StepCount { get; }
    RecipeStep GetStep(int index);
    double OverPressure { get; }
    int Time {  get; }
}

public sealed class RecipeStep
{
    public double CleanTimeSec { get; init; }      // CleanTimeValue
    public double RfPowerWatt { get; init; }       // RFPowerValue
    public double VacuumSetpointTorr { get; init; } // VacuumValue
    public double Gas1 { get; init; }
    public double Gas2 { get; init; }
    public double Gas3 { get; init; }
    public double Gas4 { get; init; }
    public double Gas5 { get; init; }
    public double Gas6 { get; init; }
    public double Gas7 { get; init; }
    public double Gas8 { get; init; }
}
