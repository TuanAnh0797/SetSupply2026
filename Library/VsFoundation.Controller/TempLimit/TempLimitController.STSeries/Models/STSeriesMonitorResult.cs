using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Models;

public class STSeriesMonitorResult
{
    /// <summary>
    /// Current measured value // D0001
    /// </summary>
    public float NPV { get; set; } = 0;

    /// <summary>
    /// Current set value //D0002
    /// </summary>
    public float NSP { get; set; } = 0;

    /// <summary>
    /// Target value  //D0003
    /// </summary>
    public float TSP { get; set; } = 0;

    /// <summary>
    /// SP number under operation ////D0005
    /// </summary>
    public eSTSeriesSetpointType SetpointType { get; set; } = eSTSeriesSetpointType.SP1;

    /// <summary>
    /// Now Status: D0010
    /// </summary>
    public STSeriesNowStatus NowStatus { get; set; } = new();

    /// <summary>
    /// Alarm Status: D0014
    /// </summary>
    public STSeriesAlarmStatus AlarmStatus { get; set; } = new();

}

/// <summary>
/// Now Status: D0010
/// </summary>
public class STSeriesNowStatus
{
    /// <summary>
    /// operation status RUN=0/STOP=1; Bit 0
    /// </summary>
    public bool IsRun { get; set; } = false;

    /// <summary>
    /// auto tuning operation; Bit 12
    /// </summary>
    public bool AT { get; set; } = false;

    /// <summary>
    /// 0:AUTO, 1:MAN; bit 13
    /// </summary>
    public bool IsAuto { get; set; } = false;


    public void SetValue(short data)
    {
        IsRun = (data >> 0 & 0x01) == 0;
        AT = (data >> 12 & 0x01) == 1;
        IsAuto = (data >> 13 & 0x01) == 0;
    }
}

/// <summary>
/// D0014
/// </summary>
public class STSeriesAlarmStatus
{
    public bool ALM1 { get; set; } = false;//Bit 0
    public bool ALM2 { get; set; } = false;//Bit 1
    public bool ALM3 { get; set; } = false;//Bit 2
    public bool ALM4 { get; set; } = false;//Bit 3
    public bool EVENT1 { get; set; } = false;//Bit 4 
    public bool EVENT2 { get; set; } = false;//Bit 5
    public bool EVENT3 { get; set; } = false;//Bit 6
    public bool EVENT4 { get; set; } = false;//Bit 7
    public bool HBA { get; set; } = false;//Bit 8
    public bool LBA { get; set; } = false;//Bit 9
    public bool TIMER1 { get; set; } = false;//Bit 10
    public bool TIMER2 { get; set; } = false;//Bit 11
    public void SetValue(short data)
    {
        ALM1 = (data >> 0 & 0x01) == 1;
        ALM2 = (data >> 1 & 0x01) == 1;
        ALM3 = (data >> 2 & 0x01) == 1;
        ALM4 = (data >> 3 & 0x01) == 1;
        EVENT1 = (data >> 4 & 0x01) == 1;
        EVENT2 = (data >> 5 & 0x01) == 1;
        EVENT3 = (data >> 6 & 0x01) == 1;
        EVENT4 = (data >> 7 & 0x01) == 1;
        HBA = (data >> 8 & 0x01) == 1;
        LBA = (data >> 9 & 0x01) == 1;
        TIMER1 = (data >> 10 & 0x01) == 1;
        TIMER2 = (data >> 11 & 0x01) == 1;
    }
}