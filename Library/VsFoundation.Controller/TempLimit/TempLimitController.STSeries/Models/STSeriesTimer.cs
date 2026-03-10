using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Models;

public class STSeriesTimerConfig
{
    public STSeriesSubTimerConfig Timer1 { get; set; } = new();
    public STSeriesSubTimerConfig Timer2 { get; set; } = new();

    public List<short> ToList()
    {
        var lst = new List<short>();
        lst.AddRange(Timer1.ToList());
        lst.AddRange(Timer2.ToList());
        return lst;
    }
}
public class STSeriesSubTimerConfig
{
    public eSTSeriesTimerSource TimerSource { get; set; } = eSTSeriesTimerSource.OFF;//TM.S
    public eSTSeriesTimerSourceType TimerSourceType { get; set; } = eSTSeriesTimerSourceType.DLY2;//TM.T
    public float Time1 { get; set; } = 0;//TM.1
    public float Time2 { get; set; } = 0;//TM.2
                                         //more ... time unit 
    public List<short> ToList()
    {
        return new List<short>() { (short)(TimerSource), (short)(TimerSourceType), (short)(Time1 * 10), (short)(Time2 * 10) };
    }

}
