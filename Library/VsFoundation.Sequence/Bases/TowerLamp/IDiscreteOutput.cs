namespace VsFoundation.Sequence.Bases.TowerLamp;

public interface IDiscreteOutput
{
    string Name { get; }
    bool IsOn { get; }
    void On();
    void Off();
}
