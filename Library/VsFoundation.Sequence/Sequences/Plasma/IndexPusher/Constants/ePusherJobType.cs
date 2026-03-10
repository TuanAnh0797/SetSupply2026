namespace VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Constants;

public enum PusherJobType
{
    LoadToChamber,
    UnloadFromChamber
}

public readonly record struct PusherDemand(int Lane, PusherJobType JobType, int StripCount);

public readonly record struct ConditionResult(bool Ok, IReadOnlyList<string> Reasons)
{
    public static ConditionResult Success() => new(true, Array.Empty<string>());
    public static ConditionResult Fail(params string[] reasons) => new(false, reasons);
}
