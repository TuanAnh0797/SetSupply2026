namespace VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Controller.Interfaces;

public interface IPushDrive
{
    bool IsHomed { get; }
    void Home();
    void MoveAbs(double pos, double vel);
    double Position { get; }
}
