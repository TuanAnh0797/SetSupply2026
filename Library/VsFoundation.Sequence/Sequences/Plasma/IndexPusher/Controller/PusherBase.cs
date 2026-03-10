using VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Configs;
using VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Controller.Interfaces;

namespace VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Controller;

public abstract class PusherBase : IPusherBase
{
    protected readonly IPushDrive _pusherDrive;
    protected readonly IPusherCylinder _pusherDn;
    protected readonly IndexPusherConfig _cfg;

    public int LaneCount => _cfg.LaneCount;

    public PusherBase(IPushDrive drive, IPusherCylinder pusherCylinder, IndexPusherConfig cfg)
    {
        _pusherDrive = drive ?? throw new ArgumentNullException(nameof(drive));
        _pusherDn = pusherCylinder ?? throw new ArgumentNullException(nameof(pusherCylinder));
        _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
    }

    public bool IsPusherDn(int lane)
    {
        return _pusherDn.IsDown(lane);
    }

    public bool IsPusherUp(int lane)
    {
        return _pusherDn.IsUp(lane);
    }

    public void MoveToLoadToChamberEnd()
    {
        _pusherDrive.MoveAbs(_cfg.LoadToChamberEndPos.Pos, _cfg.LoadToChamberEndPos.Vel);
    }

    public void MoveToLoadToChamberStart()
    {
        _pusherDrive.MoveAbs(_cfg.LoadToChamberStartPos.Pos, _cfg.LoadToChamberStartPos.Vel);
    }

    public void MoveToReady()
    {
        _pusherDrive.MoveAbs(_cfg.LoadToChamberStartPos.Pos, _cfg.LoadToChamberStartPos.Vel);
    }

    public void MoveToRetract()
    {
        _pusherDrive.MoveAbs(_cfg.RetractPos.Pos, _cfg.RetractPos.Vel);
    }

    public void MoveToUnloadFromChamberEnd()
    {
        _pusherDrive.MoveAbs(_cfg.UnloadFromChamberEndPos.Pos, _cfg.UnloadFromChamberEndPos.Vel);
    }

    public void MoveToUnloadFromChamberStart()
    {
        _pusherDrive.MoveAbs(_cfg.UnloadFromChamberStartPos.Pos, _cfg.UnloadFromChamberStartPos.Vel);
    }

    public void PusherDn(int lane)
    {
        _pusherDn.Down(lane);
    }

    public void PusherUp(int lane)
    {
        _pusherDn.Up(lane);
    }
}
