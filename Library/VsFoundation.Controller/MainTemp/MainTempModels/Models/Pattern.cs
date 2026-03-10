using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.MainTemp.MainTempModels.Enum;

namespace VsFoundation.Controller.MainTemp.MainTempModels.Models;

public class Pattern
{
    /// <summary>
    ///  Loop-1 Starting target setpoint (48003-1F42)
    /// </summary>
    /// <param >0.0 to 100.0% of PV input range (EU), (Setting range: P.RL to P.RH)</param>
    public float SSP_L1 { get; set; } = 0;
    /// <summary>
    ///  Loop-2 Starting target setpoint (48004-1F43)
    /// </summary>
    /// <param>0.0 to 100.0% of PV input range (EU), (Setting range: P.RL to P.RH)</param>        
    private float SSP_L2 { get; set; } = 0; //******* unchanged when written
    /// <summary>
    /// Start code; (48005-1F44)
    /// + 0: SSP (Program operation begins with the starting target setpoint.
    /// + 1: RAMP (Ramp-prioritized PV start)
    /// + 2: TIME (Time-prioritized PV start)
    /// + 4: LSP (Local-mode start)
    /// + 5: RSP (Remote-mode start)
    /// (* STC=TIME cannot be selected when the parameter SEG.T is TM.RT.)
    /// </summary>
    public eStartCode STC { get; set; } = 0;
    public List<short> ToListByte() { return new List<short>() { (short)(SSP_L1 * 10), (short)(SSP_L2 * 10), (short)STC }; }


    public override bool Equals(object? obj)
    {
        if (obj is Pattern other)
        {
            return SSP_L1 == other.SSP_L1 && STC == other.STC;
        }
        return false;
    }


    

}
