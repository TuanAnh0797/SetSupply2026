namespace VsFoundation.Sequence.Restore;

public class SequenceRestore
{
    public int Step { get; set; }
    public Dictionary<int, object> ServoPositions { get; set; } = new();
    public Dictionary<int, object> CylinderStates { get; set; } = new();
    public Dictionary<int, object> OtherStates { get; set; } = new();
}
