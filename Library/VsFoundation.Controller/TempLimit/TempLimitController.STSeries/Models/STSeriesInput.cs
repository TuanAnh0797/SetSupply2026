namespace VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Models;

public class STSeriesInput
{
    public eSTSeriesInputType InputType { get; set; } = eSTSeriesInputType.K1;//D0601
    public eSTSeriesInputUnit InputUnit { get; set; } = eSTSeriesInputUnit.C;//D0602
    public int InputRH1 { get; set; } = 1370;//Input range high   D0603
    public int InputRL1 { get; set; } = -200;//Input range low  D0604
    public int InputBias { get; set; } = 0; //All Bias Setting D0621
}
