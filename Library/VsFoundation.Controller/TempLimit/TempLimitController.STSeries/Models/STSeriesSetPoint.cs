namespace VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Models;

public class STSeriesSetPoint
{
    public eSTSeriesSetpointType SetpointType { get; set; } = eSTSeriesSetpointType.SP1;
    public float Setpoint1 { get; set; } = 0;
    public float Setpoint2 { get; set; } = 0;
    public float Setpoint3 { get; set; } = 0;
    public float Setpoint4 { get; set; } = 0;
    public List<short> ToList()
    {
        return new List<short>() { (short)(SetpointType), (short)(Setpoint1 * 10), (short)(Setpoint2 * 10), (short)(Setpoint3 * 10), (short)(Setpoint4 * 10) };
    }
}
public class STSeriesSetPointSlope
{
    public float IncreasingSlope { get; set; } = 0;
    public float DecreasingSlope { get; set; } = 0;
    public List<short> ToList()
    {
        return new List<short>() { (short)(IncreasingSlope * 10), (short)(DecreasingSlope * 10) };
    }
}