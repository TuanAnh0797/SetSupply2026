using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.TempLimit.TempLimitController.M74Series.Models;

public class M74SeriesMonitorResult
{
    public M74SeriesChannelMonitorResult Channel1 { get; set; } = new();
    public M74SeriesChannelMonitorResult Channel2 { get; set; } = new();
    public M74SeriesChannelMonitorResult Channel3 { get; set; } = new();
    public M74SeriesChannelMonitorResult Channel4 { get; set; } = new();
}
public class M74SeriesChannelMonitorResult
{
    /// <summary>
    /// Process value
    /// </summary>
    public float PV { get; set; } = 0;

    /// <summary>
    /// Set Value
    /// </summary>
    public float SV { get; set; } = 0;

    /// <summary>
    /// Control Output
    /// </summary>
    public float MV { get; set; } = 0;

    public M74SeriesAlarmStatus AlarmStatus { get; set; } = new();

    //....
}

public class M74SeriesAlarmStatus
{
    private short AlarmByte = 0;
    public void SetValue(short value) { AlarmByte = value; }
    public bool IsAlarm1 => ((AlarmByte >> 0) & 0x01) == 1 ? true : false;
    public bool IsAlarm2 => ((AlarmByte >> 1) & 0x01) == 1 ? true : false;
    public bool IsAlarm3 => ((AlarmByte >> 2) & 0x01) == 1 ? true : false;
    public bool IsAlarm4 => ((AlarmByte >> 3) & 0x01) == 1 ? true : false;
    public bool IsHBA => ((AlarmByte >> 4) & 0x01) == 1 ? true : false;
    public bool IsHOC => ((AlarmByte >> 5) & 0x01) == 1 ? true : false;
    public bool IsATCompleted => ((AlarmByte >> 7) & 0x01) == 1 ? true : false;

}

public enum eM74SeriesLockStatus { Off = 0, InputOutput, ReleaseAddress50To70, AllParameter }
public enum eM74SeriesChannelMode { Inactive = 0, Monitoring, Running }
public enum eM74SeriesChannelStartStop { Stop = 0, Run=1 }
public enum eM74SeriesInputType { K0 = 1, K1 = 2, T = 5, R = 6, B = 7, S = 8, N = 10, C = 12, D = 13, J = 15, E = 16, L = 17, JPT100 = 22, PT100 = 23 }
public enum eM74SeriesUnit { DegreeC = 0, DegreeF = 1 }
public enum eM74SeriesOutputType { SSR = 1,SCR=2,Relay=3 }
