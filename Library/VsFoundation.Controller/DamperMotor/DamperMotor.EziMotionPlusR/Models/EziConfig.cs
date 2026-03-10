namespace VsFoundation.Controller.DamperMotor.DamperMotor.EziMotionPlusR.Models;

public class EziConfig
{
    public byte PortNo { get; set; } = 1;
    public int RevolutionCounter { get; set; } = 10000;
    public uint BaudRate { get; set; } = 115200;
    public EziStepsConfig StepConfig { get; set; } = new();
}


public class EziStepsConfig
{
    public List<EziParamSteps> ListHomingSteps { get; set; } = new()
        {
            new() { StepType = eEziParamStepType.STEP_ORGMETHOD, Value = 0 },
            new() { StepType = eEziParamStepType.STEP_ORGSPEED, Value = 5000 },
            new() { StepType = eEziParamStepType.STEP_ORGSEARCHSPEED, Value = 1000 },
            new() { StepType = eEziParamStepType.STEP_ORGACCDECTIME, Value = 50 },
            new() { StepType = eEziParamStepType.STEP_ORGMETHOD, Value = 0 },
            new() { StepType = eEziParamStepType.STEP_ORGDIR, Value = 0 },
            new() { StepType = eEziParamStepType.STEP_ORGOFFSET, Value = 0 },
            new() { StepType = eEziParamStepType.STEP_ORGPOSITIONSET, Value = 0 },
            new() { StepType = eEziParamStepType.STEP_ORGSENSORLOGIC, Value = 0 },
        };
    public EziParamSteps StopCurrent { get; set; } = new() { StepType = eEziParamStepType.STEP_STOPCURRENT, Value = 50 };

}
public class EziParamSteps
{
    public int Value { get; set; } = 50;
    public eEziParamStepType StepType { get; set; }
}
public enum eEziParamStepType
{
    STEP_PULSEPERREVOLUTION = 0,
    STEP_AXISMAXSPEED,
    STEP_AXISSTARTSPEED,
    STEP_AXISACCTIME,
    STEP_AXISDECTIME,

    STEP_SPEEDOVERRIDE,
    STEP_JOGHIGHSPEED,
    STEP_JOGLOWSPEED,
    STEP_JOGACCDECTIME,

    STEP_ALARMLOGIC,
    STEP_RUNSTOPSIGNALLOGIC,        //SERVO_SERVOONLOGIC,
    STEP_RESETLOGIC,

    STEP_SWLMTPLUSVALUE,
    STEP_SWLMTMINUSVALUE,
    STEP_SOFTLMTSTOPMETHOD,
    STEP_HARDLMTSTOPMETHOD,
    STEP_LIMITSENSORLOGIC,

    STEP_ORGSPEED,
    STEP_ORGSEARCHSPEED,
    STEP_ORGACCDECTIME,
    STEP_ORGMETHOD,
    STEP_ORGDIR,
    STEP_ORGOFFSET,
    STEP_ORGPOSITIONSET,
    STEP_ORGSENSORLOGIC,

    STEP_STOPCURRENT,
    STEP_MOTIONDIR,

    STEP_LIMITSENSORDIR,
    STEP_ENCODERMULTIVALUE,

    STEP_ENCODERDIR,
    STEP_POSVALUECOUNTINGMETHOD,
    MAX_STEP_PARAM
};
public class EziAxisStatus
{
    public uint Value { set; get; } = 0;

    public bool FFLAG_ERRORALL => (Value & (1u << 0)) != 0;
    public bool FFLAG_HWPOSILMT => (Value & (1u << 1)) != 0;
    public bool FFLAG_HWNEGALMT => (Value & (1u << 2)) != 0;
    public bool FFLAG_SWPOGILMT => (Value & (1u << 3)) != 0;
    public bool FFLAG_SWNEGALMT => (Value & (1u << 4)) != 0;
    public bool FFLAG_RESERVED0 => (Value & (1u << 5)) != 0;
    public bool FFLAG_RESERVED1 => (Value & (1u << 6)) != 0;
    public bool FFLAG_ERRSTEPALARM => (Value & (1u << 7)) != 0;
    public bool FFLAG_ERROVERCURRENT => (Value & (1u << 8)) != 0;
    public bool FFLAG_ERROVERSPEED => (Value & (1u << 9)) != 0;
    public bool FFLAG_ERRSTEPOUT => (Value & (1u << 10)) != 0;
    public bool FFLAG_RESERVED2 => (Value & (1u << 11)) != 0;
    public bool FFLAG_ERROVERHEAT => (Value & (1u << 12)) != 0;
    public bool FFLAG_ERRBACKEMF => (Value & (1u << 13)) != 0;
    public bool FFLAG_ERRMOTORPOWER => (Value & (1u << 14)) != 0;
    public bool FFLAG_ERRLOWPOWER => (Value & (1u << 15)) != 0;
    public bool FFLAG_EMGSTOP => (Value & (1u << 16)) != 0;
    public bool FFLAG_SLOWSTOP => (Value & (1u << 17)) != 0;
    public bool FFLAG_ORIGINRETURNING => (Value & (1u << 18)) != 0;
    public bool FFLAG_RESERVED3 => (Value & (1u << 19)) != 0;
    public bool FFLAG_RESERVED4 => (Value & (1u << 20)) != 0;
    public bool FFLAG_ALARMRESET => (Value & (1u << 21)) != 0;
    public bool FFLAG_PTSTOPPED => (Value & (1u << 22)) != 0;
    public bool FFLAG_ORIGINSENSOR => (Value & (1u << 23)) != 0;
    public bool FFLAG_ZPULSE => (Value & (1u << 24)) != 0;
    public bool FFLAG_ORIGINRETOK => (Value & (1u << 25)) != 0;
    public bool FFLAG_MOTIONDIR => (Value & (1u << 26)) != 0;
    public bool FFLAG_MOTIONING => (Value & (1u << 27)) != 0;
    public bool FFLAG_MOTIONPAUSE => (Value & (1u << 28)) != 0;
    public bool FFLAG_MOTIONACCEL => (Value & (1u << 29)) != 0;
    public bool FFLAG_MOTIONDECEL => (Value & (1u << 30)) != 0;
    public bool FFLAG_MOTIONCONST => (Value & (1u << 31)) != 0;
}
public enum eStepInputBitMask
{
    LIMITP = 0x00000001,
    LIMITN = 0x00000002,
    ORIGIN = 0x00000004,
    CLEARPOSITION = 0x00000008,
    PTA0 = 0x00000010,
    PTA1 = 0x00000020,
    PTA2 = 0x00000040,
    PTA3 = 0x00000080,
    PTA4 = 0x00000100,

    PTA5 = 0x00000200,  //  USERIN6
    PTA6 = 0x00000400,  //  USERIN7
    PTA7 = 0x00000800,  //  USERIN8

    PTSTART = 0x00001000,
    STOP = 0x00002000,
    PJOG = 0x00004000,
    NJOG = 0x00008000,
    ALARMRESET = 0x00010000,
    PAUSE = 0x00040000,
    ORIGINSEARCH = 0x00080000,
    TEACHING = 0x00100000,
    ESTOP = 0x00200000,

    JPTIN0 = 0x00400000,
    JPTIN1 = 0x00800000,
    JPTIN2 = 0x01000000,
    JPTSTART = 0x02000000,

    USERIN0 = 0x04000000,
    USERIN1 = 0x08000000,
    USERIN2 = 0x10000000,
    USERIN3 = 0x20000000,
    USERIN4 = 0x40000000,
    //USERIN5 = 0x80000000,

    // USERIN6 = 0x00000200,  //  PTA5
    //  USERIN7 = 0x00000400,  //  PTA6
    //  USERIN8 = 0x00000800,  //  PTA7
}