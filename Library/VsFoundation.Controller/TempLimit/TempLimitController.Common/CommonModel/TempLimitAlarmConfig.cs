using VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonType;
using VsFoundation.Controller.TempLimit.TempLimitController.M74Series.Models;

namespace VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonModel;

public class TempLimitAlarm
{
    public eTempLimitAlarmType AlarmType { get; set; } = eTempLimitAlarmType.Off;
    public float AlarmSetValue { get; set; } = 0;
    public float AlarmDeadBand { get; set; } = 1;

    /// <summary>
    /// only using M74 series
    /// </summary>
    public eM74SeriesAlarmOutputPort AlarmOutputPort { get; set; } = eM74SeriesAlarmOutputPort.OFF;
}