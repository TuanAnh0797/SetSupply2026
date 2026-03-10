namespace VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Controller.Interfaces;

public interface IPusherWithGripper
{
    void GripperOpen(int lane);
    void GripperClose(int lane);
    bool IsGripperClosed(int lane);
}
