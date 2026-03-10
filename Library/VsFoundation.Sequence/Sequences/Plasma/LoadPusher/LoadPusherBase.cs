using Org.BouncyCastle.Asn1.BC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Sequence.Sequences.Plasma.LoadPusher.Configs;
using VsFoundation.Sequence.Sequences.Plasma.LoadPusher.Constants;
using VsFoundation.Sequence.Sequences.Plasma.LoadPusher.Interfaces;

namespace VsFoundation.Sequence.Sequences.Plasma.LoadPusher;

public class LoadPusherBase: ILoadPusherBase
{
    protected readonly ILoadPushDrive _pusherDrive;
    protected readonly ILoadPusherCondition _condition; 
    protected readonly LoadPusherConfig _cfg;
    public int LaneCount => _cfg.LaneCount; 

    public LoadPusherBase(ILoadPushDrive drive,ILoadPusherCondition PusherCondition, LoadPusherConfig cfg)
    {
        _pusherDrive = drive ?? throw new ArgumentNullException(nameof(drive));
        _condition = PusherCondition ?? throw new ArgumentNullException(nameof(PusherCondition));
        _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
    }
    public bool MoveToHome()
    {
        if (!_condition.IsMoveCondition(eLoadPusherJobType.Home))
        {
            return false;
        }
        return _pusherDrive.Home();
    }

    public bool MoveToReady()
    {
        if (!_condition.IsMoveCondition(eLoadPusherJobType.Ready))
        {
            return false;
        }
       return _pusherDrive.MoveAbs(_cfg.ReadyPos.Pos, _cfg.ReadyPos.Vel, _cfg.ReadyPos.Acc);
    }

    public bool MoveToRetract()
    {
        if (!_condition.IsMoveCondition(eLoadPusherJobType.Retract))
        {
            return false;
        }
        return _pusherDrive.MoveAbs(_cfg.RetractPos.Pos, _cfg.RetractPos.Vel,_cfg.RetractPos.Acc);
    }

    public bool MoveToExtend()
    {
        if (!_condition.IsMoveCondition(eLoadPusherJobType.Extend))
        {
            return false;
        }
        return _pusherDrive.MoveAbs(_cfg.ExtendPos.Pos, _cfg.ExtendPos.Vel, _cfg.ExtendPos.Acc);
    }

    public bool IsPusherHomed()
    {
        return _pusherDrive.IsHomed;
    }

    public bool IsPusherReady()
    {
        return _pusherDrive.IsMotorPos(_cfg.ReadyPos.Pos);
    }

    public bool IsPusherExtend()
    {
        return _pusherDrive.IsMotorPos(_cfg.ExtendPos.Pos);

    }
    public bool IsPusherRetract()
    {
        return _pusherDrive.IsMotorPos(_cfg.RetractPos.Pos);
    }

    public bool IsPushReserved()
    {
        return _condition.IsPushReserved(); 
    }

    public bool IsJobComplete()
    {
        return _condition.IsJobComplete();
    }
}
