namespace VsFoundation.Controller.TempLimit.TempLimitController.VXSeries.Models;

public class VXSeriesSUBGroup
{
    public eVXSeriesSUBType SUB1 { get; set; } = eVXSeriesSUBType.ALM1;
    public eVXSeriesSUBType SUB2 { get; set; } = eVXSeriesSUBType.ALM2;
    public List<short> ToListByte() { return new List<short>() { (short)(SUB1), (short)(SUB2) }; }

}
public enum eVXSeriesSUBType { NONE = 0, ALM1 = 1, ALM2 = 2, ALM3 = 3, ALM4 = 4, HBA = 5, LBA = 6 }

