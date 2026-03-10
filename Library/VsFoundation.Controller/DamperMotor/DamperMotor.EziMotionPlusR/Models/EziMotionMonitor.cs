using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.DamperMotor.DamperMotor.EziMotionPlusR.Models;

public class EziMotionMonitor
{
    public int CommandPosition { get; set; }
    public int ActualPosition { get; set; }
    public int PositionError { get; set; }
    public int ActualVelocity { get; set; }
    public ushort PositionTableItemNo { get; set; }

    public EziAxisStatus AlarmCode { get; set; } = new();

}