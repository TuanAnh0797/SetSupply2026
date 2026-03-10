using VsFoundation.Controller.Common.Protocol.Enum;

namespace VsFoundation.Controller.DIOBoard.DIOBoard.OvenDIOBoard.Models;

public class DOState()
{
    private eStatusCoil _y000_TowerLampRed;
    private eStatusCoil _y001_TowerLampGreen;
    private eStatusCoil _y002_TowerLampYellow;
    private eStatusCoil _y003_BuzzerPower;
    private eStatusCoil _y004_BuzzerKindRelay;
    private eStatusCoil _y005_BuzzerOffSwitchLamp;
    private eStatusCoil _y006_DryAirSol;
    private eStatusCoil _y007_NC;
    private eStatusCoil _y008_N2;
    private eStatusCoil _y009_NC;
    private eStatusCoil _y00A_DamperStepEnable;
    private eStatusCoil _y00B_AirInletStepEnable;
    private eStatusCoil _y00C_SpareRelayOut;
    private eStatusCoil _y00D_SpareTrOut1;
    private eStatusCoil _y00E_SpareTrOut2;
    private eStatusCoil _y00F_NC;

    public eStatusCoil Y000_TowerLampRed { get => _y000_TowerLampRed; set => _y000_TowerLampRed = value; }
    public eStatusCoil Y001_TowerLampGreen { get => _y001_TowerLampGreen; set => _y001_TowerLampGreen = value; }
    public eStatusCoil Y002_TowerLampYellow { get => _y002_TowerLampYellow; set => _y002_TowerLampYellow = value; }
    public eStatusCoil Y003_BuzzerPower { get => _y003_BuzzerPower; set => _y003_BuzzerPower = value; }
    public eStatusCoil Y004_BuzzerKindRelay { get => _y004_BuzzerKindRelay; set => _y004_BuzzerKindRelay = value; }
    public eStatusCoil Y005_BuzzerOffSwitchLamp { get => _y005_BuzzerOffSwitchLamp; set => _y005_BuzzerOffSwitchLamp = value; }
    public eStatusCoil Y006_DryAirSol { get => _y006_DryAirSol; set => _y006_DryAirSol = value; }
    public eStatusCoil Y007_NC { get => _y007_NC; set => _y007_NC = value; }
    public eStatusCoil Y008_N2 { get => _y008_N2; set => _y008_N2 = value; }
    public eStatusCoil Y009_NC { get => _y009_NC; set => _y009_NC = value; }
    public eStatusCoil Y00A_DamperStepEnable { get => _y00A_DamperStepEnable; set => _y00A_DamperStepEnable = value; }
    public eStatusCoil Y00B_AirInletStepEnable { get => _y00B_AirInletStepEnable; set => _y00B_AirInletStepEnable = value; }
    public eStatusCoil Y00C_SpareRelayOut { get => _y00C_SpareRelayOut; set => _y00C_SpareRelayOut = value; }
    public eStatusCoil Y00D_SpareTrOut1 { get => _y00D_SpareTrOut1; set => _y00D_SpareTrOut1 = value; }
    public eStatusCoil Y00E_SpareTrOut2 { get => _y00E_SpareTrOut2; set => _y00E_SpareTrOut2 = value; }
    public eStatusCoil Y00F_NC { get => _y00F_NC; set => _y00F_NC = value; }
}
