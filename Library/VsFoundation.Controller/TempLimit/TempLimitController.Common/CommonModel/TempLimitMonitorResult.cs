namespace VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonModel;

public class TempLimitMonitorResult
{
    public float PV { get; set; } = 0;
    public float SV { get; set; } = 0;
    public TempLimitAlarmStatus AlarmStatus { get; set; } = new();

}
public class TempLimitAlarmStatus
{
    public bool IsAlarm1 { get; set; } = false;
    public bool IsAlarm2 { get; set; } = false;
    public bool IsAlarm3 { get; set; } = false;
    public bool IsAlarm4 { get; set; } = false;
}