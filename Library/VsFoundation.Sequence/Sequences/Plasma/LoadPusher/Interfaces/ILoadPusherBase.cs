using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Sequence.Sequences.Plasma.LoadPusher.Interfaces;
public interface ILoadPusherBase
{
    int LaneCount { get; }
    bool MoveToHome();
    bool MoveToReady();
    bool MoveToRetract();
    bool MoveToExtend();
   
    bool IsPusherHomed();
    bool IsPusherReady();
    bool IsPusherRetract();
    bool IsPusherExtend();

    public bool IsPushReserved();

    public bool IsJobComplete();

}
