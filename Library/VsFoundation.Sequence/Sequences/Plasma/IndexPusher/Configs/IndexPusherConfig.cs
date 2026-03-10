namespace VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Configs;

public class IndexPusherConfig
{
    public int LaneCount { get; init; } = 1;

    public Param ReadyPos { get; init; } = new Param();

    public Param LoadToChamberStartPos { get; init; } = new Param();

    public Param LoadToChamberEndPos { get; init; } = new Param();

    public Param UnloadFromChamberStartPos { get; init; } = new Param();

    public Param UnloadFromChamberEndPos { get; init; } = new Param();

    public Param RetractPos { get; init; } = new Param();
}

public readonly record struct Param(
    double Pos,
    double Vel,
    double Acc
);