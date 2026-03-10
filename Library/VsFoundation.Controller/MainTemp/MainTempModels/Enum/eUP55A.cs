using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.MainTemp.MainTempModels.Enum;

public enum eUP55AInputType
{
    None = -1,
    OFF = 0,         // Disable
    K1 = 1,          // -270.0 to 1370.0°C / -450.0 to 2500.0°F
    K2 = 2,          // -270.0 to 1000.0°C / -450.0 to 2300.0°F
    K3 = 3,          // -200.0 to 500.0°C / -200.0 to 1000.0°F
    J = 4,           // -200.0 to 1200.0°C / -300.0 to 2300.0°F
    T1 = 5,          // -270.0 to 400.0°C / -450.0 to 750.0°F
    T2 = 6,          // 0.0 to 400.0°C / -200.0 to 750.0°F
    B = 7,           // 0.0 to 1800.0°C / 32 to 3300°F
    S = 8,           // 0.0 to 1700.0°C / 32 to 3100°F
    R = 9,           // 0.0 to 1700.0°C / 32 to 3100°F
    N = 10,          // -200.0 to 1300.0°C / -300.0 to 2400.0°F
    E = 11,          // -270.0 to 1000.0°C / -450.0 to 1800.0°F
    L = 12,          // -200.0 to 900.0°C / -300.0 to 1600.0°F
    U1 = 13,         // -200.0 to 400.0°C / -300.0 to 750.0°F
    U2 = 14,         // 0.0 to 400.0°C / -200.0 to 1000.0°F
    W = 15,          // 0.0 to 2300.0°C / 32 to 4200°F
    PL2 = 16,        // 0.0 to 1390.0°C / 32.0 to 2500.0°F
    P2040 = 17,      // 0.0 to 1900.0°C / 32 to 3400°F
    WRE = 18,        // 0.0 to 2000.0°C / 32 to 3600°F
    JPT1 = 30,       // -200.0 to 500.0°C / -300.0 to 1000.0°F
    JPT2 = 31,       // -150.0 to 150.0°C / -200.0 to 300.0°F
    PT1 = 35,        // -200.0 to 850.0°C / -300.0 to 1560.0°F
    PT2 = 36,        // -200.0 to 500.0°C / -300.0 to 1000.0°F
    PT3 = 37         // -150.0 to 150.0°C / -200.0 to 300.0°F
}
public enum eUP55AInputUnit
{
    None = 0,
    C = 1,         // (Degree Celsius)
    F = 5,         // (Degree Fahrenheit)
}
public enum eUP55AAlarmType
{
    None = -1,
    Disable = 0,                                  // Disable
    PVHighLimit = 1,                              // PV high limit
    PVLowLimit = 2,                               // PV low limit
    SPHighLimit = 3,                              // SP high limit
    SPLowLimit = 4,                               // SP low limit
    DeviationHighLimit = 5,                       // Deviation high limit
    DeviationLowLimit = 6,                        // Deviation low limit
    DeviationHighAndLowLimits = 7,                // Deviation high and low limits
    DeviationWithinHighAndLowLimits = 8,          // Deviation within high and low limits
    TargetSPHighLimit = 9,                        // Target SP high limit
    TargetSPLowLimit = 10,                        // Target SP low limit
    TargetSPDeviationHighLimit = 11,              // Target SP deviation high limit
    TargetSPDeviationLowLimit = 12,               // Target SP deviation low limit
    TargetSPDeviationHighAndLowLimits = 13,       // Target SP deviation high and low limits
    TargetSPDeviationWithinHighAndLowLimits = 14, // Target SP deviation within high and low limits
    OutHighLimit = 15,                            // OUT high limit
    OutLowLimit = 16,                             // OUT low limit
    CoolingSideOutHighLimit = 17,                 // Cooling-side OUT high limit
    CoolingSideOutLowLimit = 18,                  // Cooling-side OUT low limit
    AnalogInputPVHighLimit = 19,                  // Analog input PV high limit
    AnalogInputPVLowLimit = 20,                   // Analog input PV low limit
    AnalogInputRSPHighLimit = 21,                 // Analog input RSP high limit
    AnalogInputRSPLowLimit = 22,                  // Analog input RSP low limit
    AnalogInputAIN2HighLimit = 23,                // Analog input AIN2 high limit
    AnalogInputAIN2LowLimit = 24,                 // Analog input AIN2 low limit
    AnalogInputAIN4HighLimit = 25,                // Analog input AIN4 high limit
    AnalogInputAIN4LowLimit = 26,                 // Analog input AIN4 low limit
    FeedbackInputHighLimit = 27,                  // Feedback input high limit
    FeedbackInputLowLimit = 28,                   // Feedback input low limit
    PVVelocity = 29,                              // PV velocity
    FaultDiagnosis = 30,                          // Fault diagnosis
    Fail = 31                                     // FAIL
}
public enum eUP55ATimeUnit
{
    hour = 0, // hour:minute
    minute = 1, // minute:second
}
public enum eMainTempMode 
{
    Error = -1,
    RESET = 0, 
    PROG = 1, 
    LOCAL = 2, 
}
public enum eAdressHysteresisPVEvent : ushort
{
    PV1 = 2871,
    PV2 = 2872,
    PV3 = 2873,
    PV4 = 2874,
    PV5 = 2875,
    PV6 = 2876,
    PV7 = 2877,
    PV8 = 2878,
}
public enum eStartCode 
{ 
    SSP = 0,
    RAMP = 1, 
    TIME = 2, 
    LSP = 4, 
    RSP = 5 
}
public enum eJunctionCode 
{ 
    CONT = 0, 
    HOLD = 1, 
    LOC = 2,
    REM = 3 
}
public enum ePVEventType 
{
    Disable = 0, 
    PVHigh = 1, 
    PVLow = 2, 
    SPHigh = 3, 
    SPLow = 4, 
    DeviationHigh = 5,
    DeviationLow = 6, 
    DeviationHighLow = 7, 
    DeviationWithinHighLow = 8, 
    TargetSPHigh = 9, 
    TargetSPLow = 10, 
    TargetSPDeviationHigh = 11, 
    TargetSPDeviationLow = 12, 
    TargetSPDeviationHighLow = 13, 
    TargetSPDeviationWithinHighLow = 14, 
    OutHigh = 15, 
    OutLow = 16, 
    CoolingSideOutHigh = 17, 
    CoolingSideOutLow = 18 
}
//public enum ePVEventTypeUP550
//{
//    Disable = 0,
//    PVHigh = 1,
//    PVLow = 2,
//    DeviationHighEnergized = 3,
//    DeviationLowEnergized = 4,
//    DeviationHigh = 5,
//    DeviationLow = 6,
//    DeviationHighLow = 7,
//    DeviationWithinHighLow = 8,
//    PVHighLimit = 9,
//    PVLowLimit = 10,
    
//}



public enum eUP55AAlarm
{
    OFF = 0, PVEvent1 = 4801, PVEvent2 = 4802, PVEvent3 = 4803, PVEvent4 = 4805, PVEvent5 = 4806, PVEvent6 = 4807, PVEvent7 = 4809, PVEvent8 = 4810,
    TimeEvent1 = 4817, TimeEvent2 = 4818, TimeEvent3 = 4819, TimeEvent4 = 4821, TimeEvent5 = 4822, TimeEvent6 = 4823, TimeEvent7 = 4825, TimeEvent8 = 4826,
    TimeEvent9 = 4833, TimeEvent10 = 4834, TimeEvent11 = 4835, TimeEvent12 = 4837, TimeEvent13 = 4838, TimeEvent14 = 4839, TimeEvent15 = 4841, TimeEvent16 = 4842,
    Alarm1 = 4353,
    Alarm2 = 4354,
    Alarm3 = 4355,
    Alarm4 = 4357,
    Alarm5 = 4358,
    Alarm6 = 4359,
    Alarm7 = 4361,
    Alarm8 = 4362,
    AUTO_MAN_Status = 4177,
    ProgramRESETStatus = 4181,
    ProgramRUNStatus = 4182,
    LocalOperationStatus = 4183,
    RemoteOperationStatus = 4185,
    HOLDModeStatus = 4189,
    ProgramAdvanceStatus = 4187,
    PatternEndSignal_1s = 4265,
    PatternEndSignal_3s = 4266,
    PatternEndSignal_5s = 4267,
    WaitEndSignal_1s = 4257,
    WaitEndSignal_3s = 4258,
    WaitEndSignal_5s = 4259,
    OutputTracking_5s_SwitchingSignal = 4186
}
public enum eUP55AContactType
{
    NormalOpen = 0,
    NormalClose = 1,
}
public enum AddressSetPointAlarm
{
    Alarm1 = 2351,
    Alarm2 = 2352,
    Alarm3 = 2353,
    Alarm4 = 2354,
    Alarm5 = 2355,
    Alarm6 = 2356,
    Alarm7 = 2357,
    Alarm8 = 2358
}
public enum AddressAlarm
{
    Alarm1 = 2801,
    Alarm2 = 2805,
    Alarm3 = 2809,
    Alarm4 = 2813,
    Alarm5 = 2817,
    Alarm6 = 2821,
    Alarm7 = 2825,
    Alarm8 = 2829
}
public enum eUP55AAlarmStandByAction
{
    WithoutStandbyAction = 0, // Without standby action
    WithStandbyAction = 1, // With standby action
}
public enum eUP55AAlarmEnergized
{
    Energized = 0,
    DeEnerized = 1,
}
public enum eUP55AAlarmLatch
{
    OFF = 0,
    Latch1 = 1,
    Latch2 = 2,
    Latch3 = 3,
    Latch4 = 4,
}
public enum errorCodeReadWritePatternSegment
{
    OK = 0,
    PatternEditDuringRun = 1,              // Pattern creation or editing is disabled during program operation.
    PatternNumberNotExist = 2,             // The specified program pattern number does not exist. (Range: 1–30)
    SegmentNumberNotExist = 3,             // The specified segment number does not exist. (Range: 1–99)
    SegmentWriteExceededLimit = 22,        // The number of segments exceeded 300.
    PatternCopySourceOrDestMissing = 31,   // The copy source or destination pattern does not exist.
    PatternDeleteNotExist = 41             // The pattern to be deleted does not exist.
}
public enum eStatusCoil : byte
{
    On = 0xFF,
    Off = 0x00
}
