namespace VsFoundation.Controller.TempLimit.TempLimitController.VXSeries.Models;

public class VXSeriesALMGroup
{
    public VXSeriesAlarm Alarm1 { get; set; } = new();
    public VXSeriesAlarm Alarm2 { get; set; } = new();
    public VXSeriesAlarm Alarm3 { get; set; } = new();
    public VXSeriesAlarm Alarm4 { get; set; } = new();
    public List<short> ToListByte()
    {
        List<short> list = new();
        list.AddRange(Alarm1.ToListByte());
        list.AddRange(Alarm2.ToListByte());
        list.AddRange(Alarm3.ToListByte());
        list.AddRange(Alarm4.ToListByte());
        return list;
    }
}
public enum eVXSeriesAlarmType
{
    AlarmOFF = 0, HighAbsolute = 1, LowAbsolute = 2, HighDeviation = 3, LowDeviation = 4, HighLowDeviation = 5, HighLowRange = 6,
    HighAbsoluteWithStandbySequence = 7, LowAbsoluteWithStandbySequence = 8, HighDeviationWithStandbySequence = 9,
    LowDeviationWithStandbySequence = 10, HighLowDeviationWithStandbySequence = 11, HighLowRangeWithStandbySequence = 12,
    SensorError = 13
}
public enum eVXSeriesLatchStatus { RST = 0, SET = 1 }
public class VXSeriesAlarm
{
    public eVXSeriesAlarmType AlarmType { get; set; } = eVXSeriesAlarmType.AlarmOFF;
    public float AlarmSetValue { get; set; } = 30;
    public float AlarmDeadBand { get; set; } = 1;
    public eVXSeriesLatchStatus AlarmLS { get; set; } = eVXSeriesLatchStatus.RST;//not change
    public List<short> ToListByte() { return new List<short>() { (short)(AlarmType), (short)(AlarmSetValue), (short)AlarmDeadBand, (short)AlarmLS }; }
}