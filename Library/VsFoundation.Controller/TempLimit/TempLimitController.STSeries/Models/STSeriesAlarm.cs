namespace VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Models;

public class STSeriesAlarm
{
    public STSeriesAlarmType AlarmType { get; set; } = new();
    public STSeriesAlarmValue AlarmValue { get; set; } = new();
    public STSeriesAlarmDeadBand AlarmDeadBand { get; set; } = new();
    public STSeriesAlarmDelayTime AlarmDelayTime { get; set; } = new();
    public STSeriesAlarmUpperLowerDeviation AlarmUpperLowerDeviation { get; set; } = new();
    public STSeriesAlarmHighLowDeviation AlarmHighLowDeviation { get; set; } = new();
    public STSeriesAlarmMode AlarmMode { get; set; } = new();
}
public class STSeriesAlarmType
{
    public eSTSeriesAlarmType Alarm1 { get; set; } = eSTSeriesAlarmType.OFF;
    public eSTSeriesAlarmType Alarm2 { get; set; } = eSTSeriesAlarmType.OFF;
    public eSTSeriesAlarmType Alarm3 { get; set; } = eSTSeriesAlarmType.OFF;
    public eSTSeriesAlarmType Alarm4 { get; set; } = eSTSeriesAlarmType.OFF;

    public List<short> ToList()
    {
        return new List<short>() { (short)Alarm1, (short)Alarm2, (short)Alarm3, (short)Alarm4 };
    }
}
public class STSeriesAlarmValue
{
    public float Alarm1 { get; set; } = 0;
    public float Alarm2 { get; set; } = 0;
    public float Alarm3 { get; set; } = 0;
    public float Alarm4 { get; set; } = 0;
    public List<short> ToList()
    {
        return new List<short>() { (short)(Alarm1 * 10), (short)(Alarm2 * 10), (short)(Alarm3 * 10), (short)(Alarm4 * 10) };
    }
}
public class STSeriesAlarmDeadBand
{
    public float Alarm1 { get; set; } = 0;
    public float Alarm2 { get; set; } = 0;
    public float Alarm3 { get; set; } = 0;
    public float Alarm4 { get; set; } = 0;
    public List<short> ToList()
    {
        return new List<short>() { (short)(Alarm1 * 10), (short)(Alarm2 * 10), (short)(Alarm3 * 10), (short)(Alarm4 * 10) };
    }
}
public class STSeriesAlarmMode
{
    public eSTSeriesAlarmMode Alarm1 { get; set; } = eSTSeriesAlarmMode.RUN;
    public eSTSeriesAlarmMode Alarm2 { get; set; } = eSTSeriesAlarmMode.RUN;
    public eSTSeriesAlarmMode Alarm3 { get; set; } = eSTSeriesAlarmMode.RUN;
    public eSTSeriesAlarmMode Alarm4 { get; set; } = eSTSeriesAlarmMode.RUN;
    public List<short> ToList()
    {
        return new List<short>() { (short)(Alarm1), (short)(Alarm2), (short)(Alarm3), (short)(Alarm4) };
    }
}
public class STSeriesAlarmDelayTime
{
    public float Alarm1 { get; set; } = 0;//(mm.ss)
    public float Alarm2 { get; set; } = 0;//(mm.ss)
    public float Alarm3 { get; set; } = 0;//(mm.ss)
    public float Alarm4 { get; set; } = 0;//(mm.ss)
    public List<short> ToList()
    {
        return new List<short>() { (short)(Alarm1 * 10), (short)(Alarm2 * 10), (short)(Alarm3 * 10), (short)(Alarm4 * 10) };
    }
}
public class STSeriesAlarmUpperLowerDeviation
{
    public float Alarm1Upper { get; set; } = 0;
    public float Alarm2Upper { get; set; } = 0;
    public float Alarm3Upper { get; set; } = 0;
    public float Alarm4Upper { get; set; } = 0;
    public float Alarm1Lower { get; set; } = 0;
    public float Alarm2Lower { get; set; } = 0;
    public float Alarm3Lower { get; set; } = 0;
    public float Alarm4Lower { get; set; } = 0;
    public List<short> ToList()
    {
        return new List<short>() { (short)(Alarm1Upper * 10), (short)(Alarm2Upper * 10), (short)(Alarm3Upper * 10), (short)(Alarm4Upper * 10), 0, (short)(Alarm1Lower * 10), (short)(Alarm2Lower * 10), (short)(Alarm3Lower * 10), (short)(Alarm4Lower * 10) };
    }
}
public class STSeriesAlarmHighLowDeviation
{
    public float Alarm1High { get; set; } = 0;
    public float Alarm2High { get; set; } = 0;
    public float Alarm3High { get; set; } = 0;
    public float Alarm4High { get; set; } = 0;
    public float Alarm1Low { get; set; } = 0;
    public float Alarm2Low { get; set; } = 0;
    public float Alarm3Low { get; set; } = 0;
    public float Alarm4Low { get; set; } = 0;
    public List<short> ToList()
    {
        return new List<short>() { (short)(Alarm1High * 10), (short)(Alarm2High * 10), (short)(Alarm3High * 10), (short)(Alarm4High * 10), 0, (short)(Alarm1Low * 10), (short)(Alarm2Low * 10), (short)(Alarm3Low * 10), (short)(Alarm4Low * 10) };
    }
}