namespace VsFoundation.Controller.TempLimit.TempLimitController.M74Series.Models;

public class M74SeriesSVGroup
{
    public eM74SeriesSVNumber Number { get; set; } = eM74SeriesSVNumber.SV0;
    public float Value0 { get; set; } = 0;
    public float Value1 { get; set; } = 0;
    public float Value2 { get; set; } = 0;
    public float Value3 { get; set; } = 0;
    public List<short> ToListByte() { return new List<short>() { (short)(Number), (short)Value0,(short)Value1, (short)Value2, (short)Value3}; }

}
public enum eM74SeriesSVNumber { SV0 = 0, SV1 = 1, SV2 = 2, SV3 = 3 };
