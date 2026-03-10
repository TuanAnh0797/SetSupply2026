using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.TempLimit.TempLimitController.M74Series.Models;

public class M74SeriesALMGroup
{
    public M74SeriesRelayControl RelayControl { get; set; } = new();

    public VM74SeriesAlarm Alarm1 { get; set; } = new();
    public VM74SeriesAlarm Alarm2 { get; set; } = new();
    public VM74SeriesAlarm Alarm3 { get; set; } = new();
    public VM74SeriesAlarm Alarm4 { get; set; } = new();
    public List<short> ToListByte()
    {
        List<short> list = new();
        list.Add((short)RelayControl.GetByteValue());
        list.Add(0);
        list.AddRange(new short[] { (short)Alarm1.AlarmType, (short)Alarm2.AlarmType, (short)Alarm3.AlarmType, (short)Alarm4.AlarmType });
        list.AddRange(new short[] { (short)Alarm1.AlarmDeadBand, (short)Alarm2.AlarmDeadBand, (short)Alarm3.AlarmDeadBand, (short)Alarm4.AlarmDeadBand });
        list.AddRange(new short[] { (short)Alarm1.AlarmSetValue, (short)Alarm2.AlarmSetValue, (short)Alarm3.AlarmSetValue, (short)Alarm4.AlarmSetValue });
        list.AddRange(new short[] { (short)Alarm1.AlarmOutputPort, (short)Alarm2.AlarmOutputPort, (short)Alarm3.AlarmOutputPort, (short)Alarm4.AlarmOutputPort });
        return list;
    }
}
public enum eM74SeriesAlarmType
{
    Off = 0,
    AbsoluteUpperAH = 1,
    AbsoluteLowerAH = 2,
    UpperDeviationAH = 3,
    LowerDeviationAH = 4,
    UpperDeviationAL = 5,
    LowerDeviationAL = 6,
    UpperLowerDeviationAH = 7,
    UpperLowerDeviationInRangeAH = 8,
    AbsoluteUpperAL = 9,
    AbsoluteLowerAL = 10,
    //More...

}
public enum eM74SeriesAlarmOutputPort { OFF = 0, ALM1Port = 1, ALM2Port = 2, ALM3Port = 3, ALM4Port = 4, }
public class VM74SeriesAlarm
{
    public eM74SeriesAlarmType AlarmType { get; set; } = eM74SeriesAlarmType.Off;
    public float AlarmSetValue { get; set; } = 0;
    public float AlarmDeadBand { get; set; } = 1;
    public eM74SeriesAlarmOutputPort AlarmOutputPort { get; set; } = eM74SeriesAlarmOutputPort.OFF;
    public List<short> ToListByte()
    {
        List<short> list = new();

        return list;
    }

}
public class M74SeriesRelayControl
{
    public bool IsOnAlarm2 { get; set; } = false;
    public bool IsOnAlarm3 { get; set; } = false;
    public bool IsOnAlarm4 { get; set; } = false;
    public void SetValueFromByte(in short value)
    {
        IsOnAlarm2 = (value >> 0 & 0x01) == 1 ? true : false;
        IsOnAlarm3 = (value >> 1 & 0x01) == 1 ? true : false;
        IsOnAlarm4 = (value >> 2 & 0x01) == 1 ? true : false;
    }
    public short GetByteValue()
    {
        byte ret = 0;
        ret |= (byte)((IsOnAlarm2 ? 1 : 0) << 0);
        ret |= (byte)((IsOnAlarm3 ? 1 : 0) << 1);
        ret |= (byte)((IsOnAlarm4 ? 1 : 0) << 2);
        return ret;
    }
}
