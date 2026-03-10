namespace VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Controller.Interfaces;

public interface IGripper
{
    void Open(int lane);
    void Close(int lane);
    bool IsClosed(int lane);
}
