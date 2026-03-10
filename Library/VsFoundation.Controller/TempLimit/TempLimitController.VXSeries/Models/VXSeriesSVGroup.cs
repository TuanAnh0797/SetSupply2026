namespace VsFoundation.Controller.TempLimit.TempLimitController.VXSeries.Models;

public class VXSeriesSVGroup
{
    public eVXSeriesSVNumber Number { get; set; } = eVXSeriesSVNumber.SV1;//100//main
    public float HighLimit { get; set; } = 1370;//101
    public float LowLimit { get; set; } = -200;//102
    public float Value1 { get; set; } = -200;//103//main
    public float Value2 { get; set; } = -200;//104
    public float Value3 { get; set; } = -200;//105
    public float Value4 { get; set; } = -200;//106
    public List<short> ToListByte() { return new List<short>() { (short)(Number), (short)(HighLimit), (short)LowLimit, (short)Value1, (short)Value2, (short)Value3, (short)Value4 }; }

}
public enum eVXSeriesSVNumber { SV1 = 1, SV2 = 2, SV3 = 3, SV4 = 4 };

