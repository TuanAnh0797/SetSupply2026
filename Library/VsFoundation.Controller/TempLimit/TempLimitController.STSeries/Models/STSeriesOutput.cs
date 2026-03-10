namespace VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Models;

public class STSeriesOutput
{
    public eSTSeriesAnalogOutputType Output1AnalogType { get; set; } = eSTSeriesAnalogOutputType.HEAT;//D0624 Set OUT1 operation
    public eSTSeriesAnalogOutputType Output2AnalogType { get; set; } = eSTSeriesAnalogOutputType.RET;//D0625 Set OUT2 operation
    public eSTSeriesOutputType OutputHeat1Type { get; set; } = eSTSeriesOutputType.SSR; //D0631 Set output type for OUT1(Heating)
    public eSTSeriesOutputType OutputHeat2Type { get; set; } = eSTSeriesOutputType.SSR; //D0633 Set output type for OUT2(Heating)
    public eSTSeriesOutputType OutputCool1Type { get; set; } = eSTSeriesOutputType.SSR;//D0632 Set output type for OUT1 (Cooling)
    public eSTSeriesOutputType OutputCool2Type { get; set; } = eSTSeriesOutputType.SSR;//D0634 Set output type for OUT2 (Cooling)

    public eSTSeriesEventType EVent1Type { get; set; } = eSTSeriesEventType.RUN;//D0627
    public eSTSeriesEventType EVent2Type { get; set; } = eSTSeriesEventType.TMR1;//D0628
    public eSTSeriesEventType EVent3Type { get; set; } = eSTSeriesEventType.TMR2;//D0629
    public eSTSeriesEventType EVent4Type { get; set; } = eSTSeriesEventType.ALM1;//D0630

    public List<short> ToList()
    {
        var lst = new List<short>();
        lst.AddRange(new List<short>() { (short)Output1AnalogType, (short)Output1AnalogType, 0 });
        lst.AddRange(new List<short>() { (short)EVent1Type, (short)EVent2Type, (short)EVent3Type, (short)EVent4Type });
        lst.AddRange(new List<short>() { (short)OutputHeat1Type, (short)OutputCool1Type, (short)OutputHeat2Type, (short)OutputCool2Type });
        return lst;
    }
}
