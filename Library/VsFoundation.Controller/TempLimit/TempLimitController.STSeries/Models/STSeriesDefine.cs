using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Models;

public enum eSTSeriesProcessTimerMode
{
    /// <summary>
    /// PV.SP : After RUN, P-TM will proceed from PV = SP
    /// </summary>
    PV_SP = 0,

    /// <summary>
    /// After S-TM operation, P-TM will proceed.
    /// </summary>
    S_TM = 1
}
public enum eSTSeriesInputType
{
    K1 = 1, K2 = 2, J = 3, E = 4, T = 5, R = 6, B = 7,
    S = 8, L = 9, N = 10, U = 11, W = 12, Platinum = 13, C = 14, PTA = 15, PTB = 16, PTC = 17, PTD = 18, JPTA = 19, JPTB = 20,
    V2V, V5V, V10V, V20MV, V100MV
}
public enum eSTSeriesInputUnit
{
    C = 0,         // (Degree Celsius)
    F = 1,         // (Degree Fahrenheit)
}
public enum eSTSeriesAnalogOutputType
{
    COOL = 0,
    HEAT = 1,
    RET = 2
}
public enum eSTSeriesOutputType
{
    SSR = 0,
    SCR = 1,
}
public enum eSTSeriesEventType
{
    COOL = 0,
    HEAT,
    ALM1,
    ALM2,
    ALM3,
    ALM4,
    RUN,
    IS1,
    IS2,
    LBA,
    TMR1,
    TMR2
}



public enum eSTSeriesSetpointType
{
    None = 0,
    SP1 = 1,
    SP2 = 2,
    SP3 = 3,
    SP4 = 4,
}

/// <summary>
///- Output Direction mode (Forward : ON when alarm condition, OFF when alarm off; - Reverse : OFF when alarm condition, ON when alarm off)
///- The condition of Standby (ON/OFF)
/// </summary>
public enum eSTSeriesAlarmType
{
    OFF = 0,
    UpperOfPV = 1,
    LowerOfPV = 2,
    UpperOfDeviation = 3,
    LowerOfDeviation = 4,
    UpperOfDeviationReverse = 5,
    LowerOfDeviationReverse = 6,
    HighLowDeviationOutOfRange = 7,
    HighLowDeviationWithinOfRange = 8,
    UpperOfPVReverse = 9,
    LowerOfPVReverse = 10,
    UpperOfValve = 11,
    LowerOfValve = 12,
    //
    //more Standby On ...
    //
    UpperOfTSP = 25,
    LowerOfTSP = 26,
    SOAK = 27,
    SOAKReverse = 28,
}
public enum eSTSeriesAlarmMode
{
    /// <summary>
    /// The alarm mode is always executed irrespective of the operation / stop.
    /// </summary>
    ALWA = 0,

    /// <summary>
    /// Alarm mode is only executed during operation
    /// </summary>
    RUN = 1,
}
public enum eSTSeriesTimerSource
{
    OFF = 0,
    /// <summary>
    ///  Execute RUN / STOP
    /// </summary>
    RUN,
    /// <summary>
    /// Execute DI1,2 ON / OFF
    /// </summary>
    DI1, DI2
}

public enum eSTSeriesTimerSourceType
{
    DLY1 = 0, DLY2, FLK1, FLK2, SOAK
}

