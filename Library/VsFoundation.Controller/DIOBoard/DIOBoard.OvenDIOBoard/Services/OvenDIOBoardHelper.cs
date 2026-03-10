using VsFoundation.Controller.Common.Protocol.Enum;

namespace VsFoundation.Controller.DIOBoard.DIOBoard.OvenDIOBoard.Models;

public static class OvenDIOBoardHelper
{
    public static bool ParseMonitorData(in List<short> lstData, out DIOState dIOState)
    {
        dIOState = new();
        //// DI00 - DI07
        var dataTerminal1 = lstData[6];
        dIOState.DiState.X000_EmergencySwitch = ((dataTerminal1 & (1 << 0)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X001_BuzzerOffSwitch = ((dataTerminal1 & (1 << 1)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X002_DoorOpenCheck = ((dataTerminal1 & (1 << 2)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X003_DoorCloseCheck = ((dataTerminal1 & (1 << 3)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X004_AirPressure = ((dataTerminal1 & (1 << 4)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X005_N2Pressure = ((dataTerminal1 & (1 << 5)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X006_HeaterOverload = ((dataTerminal1 & (1 << 6)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X007_BlowerOverload = ((dataTerminal1 & (1 << 7)) != 0) ? eStatusCoil.On : eStatusCoil.Off;

        //// DI08 - DI0F
        var dataTerminal2 = lstData[7];
        dIOState.DiState.X008_BlowerMotorLowCurrent = ((dataTerminal2 & (1 << 0)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X009_ChamberOverTemp = ((dataTerminal2 & (1 << 1)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X00A_DamperStepAlarm = ((dataTerminal2 & (1 << 2)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X00B_AirInletStepAlarm = ((dataTerminal2 & (1 << 3)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X00C_DifferentialPressure = ((dataTerminal2 & (1 << 4)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X00D_HeatSinkTempCheck = ((dataTerminal2 & (1 << 5)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X00E_ChamberInside65CTempCheck = ((dataTerminal2 & (1 << 6)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X00F_ChamberWaterFlowCheck = ((dataTerminal2 & (1 << 7)) != 0) ? eStatusCoil.On : eStatusCoil.Off;

        //// DI20 - DI17
        var dataTerminal3 = lstData[8];
        dIOState.DiState.X010_ElectronicsDoorLockStatusCheck = ((dataTerminal3 & (1 << 0)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X011_O2AnalyzerAlarm = ((dataTerminal3 & (1 << 1)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X012_GroundCheck = ((dataTerminal3 & (1 << 2)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X013_DamperOpen = ((dataTerminal3 & (1 << 3)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X014_DamperClose = ((dataTerminal3 & (1 << 4)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X015_ChamberInsideShelfCheck = ((dataTerminal3 & (1 << 5)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X016_ChamberCloseCheck = ((dataTerminal3 & (1 << 6)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X017_SpareInput1 = ((dataTerminal3 & (1 << 7)) != 0) ? eStatusCoil.On : eStatusCoil.Off;

        //// DI18 - DI1D
        var dataTerminal4 = lstData[9];
        dIOState.DiState.X018_SpareInput2 = ((dataTerminal4 & (1 << 0)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X019_SpareInput3 = ((dataTerminal4 & (1 << 1)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X01A_SpareInput4 = ((dataTerminal4 & (1 << 2)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X01B_SpareInput5 = ((dataTerminal4 & (1 << 3)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X01C_SpareInput6 = ((dataTerminal4 & (1 << 4)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DiState.X01D_SpareInput7 = ((dataTerminal4 & (1 << 5)) != 0) ? eStatusCoil.On : eStatusCoil.Off;


        // DO00 - DO07
        var dataTerminalDO1 = lstData[0];
        dIOState.DoState.Y000_TowerLampRed = ((dataTerminalDO1 & (1 << 0)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y001_TowerLampGreen = ((dataTerminalDO1 & (1 << 1)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y002_TowerLampYellow = ((dataTerminalDO1 & (1 << 2)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y003_BuzzerPower = ((dataTerminalDO1 & (1 << 3)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y004_BuzzerKindRelay = ((dataTerminalDO1 & (1 << 4)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y005_BuzzerOffSwitchLamp = ((dataTerminalDO1 & (1 << 5)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y006_DryAirSol = ((dataTerminalDO1 & (1 << 6)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y007_NC = ((dataTerminalDO1 & (1 << 7)) != 0) ? eStatusCoil.On : eStatusCoil.Off;


        // DO08 - DO0F
        var dataTerminalDO2 = lstData[1];
        dIOState.DoState.Y008_N2 = ((dataTerminalDO2 & (1 << 0)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y009_NC = ((dataTerminalDO2 & (1 << 1)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y00A_DamperStepEnable = ((dataTerminalDO2 & (1 << 2)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y00B_AirInletStepEnable = ((dataTerminalDO2 & (1 << 3)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y00C_SpareRelayOut = ((dataTerminalDO2 & (1 << 4)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y00D_SpareTrOut1 = ((dataTerminalDO2 & (1 << 5)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y00E_SpareTrOut2 = ((dataTerminalDO2 & (1 << 6)) != 0) ? eStatusCoil.On : eStatusCoil.Off;
        dIOState.DoState.Y00F_NC = ((dataTerminalDO2 & (1 << 7)) != 0) ? eStatusCoil.On : eStatusCoil.Off;

        return true;
    }

}
