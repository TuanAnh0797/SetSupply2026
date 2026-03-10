using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Sequence.Sequences.Plasma.LoadPusher.Configs;
public class LoadPusherConfig
{
    public int LaneCount { get; init; } = 1;
    public Param ReadyPos { get; init; } = new Param();

    public Param ExtendPos { get; init; } = new Param();

    public Param RetractPos { get; init; } = new Param();

    public int ServorX_AlarmStartCode{ get; init; }

}


public readonly record struct Param
(
    double Pos,
    double Vel,
    double Acc
);