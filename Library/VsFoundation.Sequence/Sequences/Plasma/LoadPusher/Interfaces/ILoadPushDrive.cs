using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Sequence.Sequences.Plasma.LoadPusher.Interfaces;
public interface ILoadPushDrive
{
    bool IsHomed { get; }
    bool Home();
    bool MoveAbs(double pos, double vel, double acc);
    bool IsMotorPos(double pos);

}
