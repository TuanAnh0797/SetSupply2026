namespace VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Controller.Interfaces;

public interface IPusherBase
{
    int LaneCount { get; }

    void MoveToReady();
    void MoveToRetract();
    void MoveToLoadToChamberStart();
    void MoveToLoadToChamberEnd();
    void MoveToUnloadFromChamberStart();
    void MoveToUnloadFromChamberEnd();

    void PusherUp(int lane);
    void PusherDn(int lane);

    bool IsPusherUp(int lane);
    bool IsPusherDn(int lane);
}
