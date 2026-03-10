using VsFoundation.Controller.Common.Protocol.Enum;

namespace VsFoundation.Controller.DIOBoard.DIOBoard.OvenDIOBoard.Models;

public class DIState()
{
    private eStatusCoil _x000_EmergencySwitch;
    private eStatusCoil _x001_BuzzerOffSwitch;
    private eStatusCoil _x002_DoorOpenCheck;
    private eStatusCoil _x003_DoorCloseCheck;
    private eStatusCoil _x004_AirPressure;
    private eStatusCoil _x005_N2Pressure;
    private eStatusCoil _x006_HeaterOverload;
    private eStatusCoil _x007_BlowerOverload;

    private eStatusCoil _x008_BlowerMotorLowCurrent;
    private eStatusCoil _x009_ChamberOverTemp;
    private eStatusCoil _x00A_DamperStepAlarm;
    private eStatusCoil _x00B_AirInletStepAlarm;
    private eStatusCoil _x00C_DifferentialPressure;
    private eStatusCoil _x00D_HeatSinkTempCheck;
    private eStatusCoil _x00E_ChamberInside65CTempCheck;
    private eStatusCoil _x00F_ChamberWaterFlowCheck;

    private eStatusCoil _x010_ElectronicsDoorLockStatusCheck;
    private eStatusCoil _x011_O2AnalyzerAlarm;
    private eStatusCoil _x012_GroundCheck;
    private eStatusCoil _x013_DamperOpen;
    private eStatusCoil _x014_DamperClose;
    private eStatusCoil _x015_ChamberInsideShelfCheck;
    private eStatusCoil _x016_ChamberCloseCheck; // Door Safety Sensor
    private eStatusCoil _x017_SpareInput1;

    private eStatusCoil _x018_SpareInput2;
    private eStatusCoil _x019_SpareInput3;
    private eStatusCoil _x01A_SpareInput4;
    private eStatusCoil _x01B_SpareInput5;
    private eStatusCoil _x01C_SpareInput6;
    private eStatusCoil _x01D_SpareInput7;

    public eStatusCoil X000_EmergencySwitch { get => _x000_EmergencySwitch; set => _x000_EmergencySwitch = value; }
    public eStatusCoil X001_BuzzerOffSwitch { get => _x001_BuzzerOffSwitch; set => _x001_BuzzerOffSwitch = value; }
    public eStatusCoil X002_DoorOpenCheck { get => _x002_DoorOpenCheck; set => _x002_DoorOpenCheck = value; }
    public eStatusCoil X003_DoorCloseCheck { get => _x003_DoorCloseCheck; set => _x003_DoorCloseCheck = value; }
    public eStatusCoil X004_AirPressure { get => _x004_AirPressure; set => _x004_AirPressure = value; }
    public eStatusCoil X005_N2Pressure { get => _x005_N2Pressure; set => _x005_N2Pressure = value; }
    public eStatusCoil X006_HeaterOverload { get => _x006_HeaterOverload; set => _x006_HeaterOverload = value; }
    public eStatusCoil X007_BlowerOverload { get => _x007_BlowerOverload; set => _x007_BlowerOverload = value; }
    public eStatusCoil X008_BlowerMotorLowCurrent { get => _x008_BlowerMotorLowCurrent; set => _x008_BlowerMotorLowCurrent = value; }
    public eStatusCoil X009_ChamberOverTemp { get => _x009_ChamberOverTemp; set => _x009_ChamberOverTemp = value; }
    public eStatusCoil X00A_DamperStepAlarm { get => _x00A_DamperStepAlarm; set => _x00A_DamperStepAlarm = value; }
    public eStatusCoil X00B_AirInletStepAlarm { get => _x00B_AirInletStepAlarm; set => _x00B_AirInletStepAlarm = value; }
    public eStatusCoil X00C_DifferentialPressure { get => _x00C_DifferentialPressure; set => _x00C_DifferentialPressure = value; }
    public eStatusCoil X00D_HeatSinkTempCheck { get => _x00D_HeatSinkTempCheck; set => _x00D_HeatSinkTempCheck = value; }
    public eStatusCoil X00E_ChamberInside65CTempCheck { get => _x00E_ChamberInside65CTempCheck; set => _x00E_ChamberInside65CTempCheck = value; }
    public eStatusCoil X00F_ChamberWaterFlowCheck { get => _x00F_ChamberWaterFlowCheck; set => _x00F_ChamberWaterFlowCheck = value; }
    public eStatusCoil X010_ElectronicsDoorLockStatusCheck { get => _x010_ElectronicsDoorLockStatusCheck; set => _x010_ElectronicsDoorLockStatusCheck = value; }
    public eStatusCoil X011_O2AnalyzerAlarm { get => _x011_O2AnalyzerAlarm; set => _x011_O2AnalyzerAlarm = value; }
    public eStatusCoil X012_GroundCheck { get => _x012_GroundCheck; set => _x012_GroundCheck = value; }
    public eStatusCoil X013_DamperOpen { get => _x013_DamperOpen; set => _x013_DamperOpen = value; }
    public eStatusCoil X014_DamperClose { get => _x014_DamperClose; set => _x014_DamperClose = value; }
    public eStatusCoil X015_ChamberInsideShelfCheck { get => _x015_ChamberInsideShelfCheck; set => _x015_ChamberInsideShelfCheck = value; }
    public eStatusCoil X016_ChamberCloseCheck { get => _x016_ChamberCloseCheck; set => _x016_ChamberCloseCheck = value; }
    public eStatusCoil X017_SpareInput1 { get => _x017_SpareInput1; set => _x017_SpareInput1 = value; }
    public eStatusCoil X018_SpareInput2 { get => _x018_SpareInput2; set => _x018_SpareInput2 = value; }
    public eStatusCoil X019_SpareInput3 { get => _x019_SpareInput3; set => _x019_SpareInput3 = value; }
    public eStatusCoil X01A_SpareInput4 { get => _x01A_SpareInput4; set => _x01A_SpareInput4 = value; }
    public eStatusCoil X01B_SpareInput5 { get => _x01B_SpareInput5; set => _x01B_SpareInput5 = value; }
    public eStatusCoil X01C_SpareInput6 { get => _x01C_SpareInput6; set => _x01C_SpareInput6 = value; }
    public eStatusCoil X01D_SpareInput7 { get => _x01D_SpareInput7; set => _x01D_SpareInput7 = value; }
}
