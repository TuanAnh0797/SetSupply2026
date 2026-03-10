namespace VsFoundation.Controller.DIOBoard.DIOBoard.OvenDIOBoard.Models;

public class DIOState
{
    public DIState DiState { get; set; } = new ();
    public DOState DoState { get; set; } = new();

}
//public enum eStatusCoil : byte
//{
//    On = 0xFF,
//    Off = 0x00
//}
public enum eOutputCoil
{
    Y000_TowerLampRed = 0,
    Y001_TowerLampGreen = 1,
    Y002_TowerLampYellow = 2,
    Y003_BuzzerPower = 3,
    Y004_BuzzerKindRelay = 4,
    Y005_BuzzerOffSwitchLamp = 5,
    Y006_DryAirSol = 6,
    Y007_Door_Lock = 7,
    Y008_N2 = 8,
    Y009_NC = 9,
    Y00A_DamperStepEnable = 10,
    Y00B_AirInletStepEnable = 11,
    Y00C_SpareRelayOut = 12,
    Y00D_SpareTrOut1 = 13,
    Y00E_HEATER_PROBLEM_LAMP = 14,
    Y00F_COOLING_WATER_VENT = 15
}