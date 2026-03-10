namespace VsFoundation.Controller.O2.O2Controller.Common;


public enum eO2ControllerType { ZR5, TorayLC850KS, TorayLC850KD, TorayLC300, TorayLC450, TorayLC860 }
public enum eO2Unit { PPM, PPB, Percent, Atm }

public class O2Result
{
    public float? Oxygen { get; set; } = 0;
    public eO2Unit Unit { get; set; } = eO2Unit.PPM;
}
