using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Sequence.Sequences.Plasma.LoadPusher.Constants;

namespace VsFoundation.Sequence.Sequences.Plasma.LoadPusher.Interfaces;

public interface ILoadPusherCondition
{
    bool IsMoveCondition(eLoadPusherJobType jobType);

    bool IsPushReserved();

    bool IsJobComplete();
}
