using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.MainTemp.MainTempModels.Models;

public class MonitorResult
{
    public UP550ADConverterErrorStatus UP550ADConverterErrorStatus { get; set; } = new();

    /// <summary>
    /// 2001
    /// </summary>
    public UP55AADConverterErrorStatus ADConverterErrorStatus { get; set; } = new();

    /// <summary>
    /// Loop-1 measurement value - 2003
    /// </summary>
    public float PV_L1 { get; set; } = 0;

    /// <summary>
    /// Loop-1 control setpoint - 2004
    /// </summary>
    public float CSP_L1 { get; set; } = 0;
    /// <summary>
    /// SP series
    /// </summary>
    public float TSP_L1 { get; set; } = 0;

    /// <summary>
    /// Loop-1 control output - 2005
    /// </summary>
    public float OUT_L1 { get; set; } = 0;

    /// <summary>
    /// Current program pattern number - D2015 //0 to 30
    /// </summary>
    public int CurrentPatternNo { get; set; } = 0;

    /// <summary>
    /// Current segment number currently in operation - D2016 //0: Not executing, 1 to 99
    /// </summary>
    public int CurrentSegmentNo { get; set; } = 0;

    /// <summary>
    ///Remaining segment-time during operation/Elapsed time during wait.t - D2017
    /// </summary>
    public TimeSpan RemainingSegmentTime { get; set; } = new TimeSpan(0);

    /// <summary>
    /// PV event status 2039
    /// </summary>
    public UP55APVEventStatus PVEventStatus { get; set; } = new();

    /// <summary>
    /// Time event status 2040
    /// </summary>
    public UP55ATimeEventStatus TimeEventStatus { get; set; } = new();

    /// <summary>
    /// Number of segments contained in the selected pattern  D2044
    /// </summary>
    public int SegmentInSelectedPattern { get; set; } = 0;
    /// <summary>
    /// 
    /// </summary>
    public bool IsTimeOut { get; set; } = false;


}
public class UP55AADConverterErrorStatus
{
    /// <summary>
    /// PV input A/D converter error //bit0
    /// </summary>
    public bool ADERR { get; set; } = false;

    /// <summary>
    /// RSP input (E1-terminal area) A/D converter error//bit1
    /// </summary>
    public bool ADERR_E1 { get; set; } = false;

    /// <summary>
    /// AIN2 input (E2-terminal area) A/D converter error//bit2
    /// </summary>
    public bool ADERR_E2 { get; set; } = false;

    /// <summary>
    /// AIN4 input (E4-terminal area) A/D converter error//bit4
    /// </summary>
    public bool ADERR_E4 { get; set; } = false;

    /// <summary>
    /// PV input RJC error//bit5
    /// </summary>
    public bool RJCERR { get; set; } = false;

    /// <summary>
    /// RSP input (E1-terminal area) burnout error//bit6
    /// </summary>
    /// 
    public bool RJCERR_E1 { get; set; } = false;

    /// <summary>
    /// PV input burnout error//bit 8
    /// </summary>
    public bool ADBO { get; set; } = false;


    /// <summary>
    /// RSP input (E1-terminal area) burnout error //bit 9
    /// </summary>
    public bool ADBO_E1 { get; set; } = false;

    /// <summary>
    /// AIN2 input (E2-terminal area) burnout error//bit10
    /// </summary>
    public bool ADBO_E2 { get; set; } = false;


    /// <summary>
    /// AIN4 input (E4-terminal area) burnout error//bit12
    /// </summary>
    public bool ADBO_E4 { get; set; } = false;
    public void SetValue(short data)
    {
        ADERR = (data >> 0 & 0x01) == 1;
        ADERR_E1 = (data >> 1 & 0x01) == 1;
        ADERR_E2 = (data >> 2 & 0x01) == 1;
        ADERR_E4 = (data >> 4 & 0x01) == 1;
        RJCERR = (data >> 5 & 0x01) == 1;
        RJCERR_E1 = (data >> 6 & 0x01) == 1;
        ADBO = (data >> 8 & 0x01) == 1;
        ADBO_E1 = (data >> 9 & 0x01) == 1;
        ADBO_E2 = (data >> 10 & 0x01) == 1;
        ADBO_E4 = (data >> 12 & 0x01) == 1;

    }
}
/// <summary>
/// 2039
/// </summary>
public class UP55APVEventStatus
{
    /// <summary>
    /// 0: PV event is OFF,1: PV event is ON; bit0
    /// </summary>
    public bool PV_EV1 { get; set; } = false;
    /// <summary>
    /// 0: PV event is OFF,1: PV event is ON; bit1
    /// </summary>
    public bool PV_EV2 { get; set; } = false;
    /// <summary>
    /// 0: PV event is OFF,1: PV event is ON; bit2
    /// </summary>
    public bool PV_EV3 { get; set; } = false;
    /// <summary>
    /// 0: PV event is OFF,1: PV event is ON; bit4
    /// </summary>
    public bool PV_EV4 { get; set; } = false;
    /// <summary>
    /// 0: PV event is OFF,1: PV event is ON; bit5
    /// </summary>
    public bool PV_EV5 { get; set; } = false;
    /// <summary>
    /// 0: PV event is OFF,1: PV event is ON; bit6
    /// </summary>
    public bool PV_EV6 { get; set; } = false;
    /// <summary>
    /// 0: PV event is OFF,1: PV event is ON; bit8
    /// </summary>
    public bool PV_EV7 { get; set; } = false;
    /// <summary>
    /// 0: PV event is OFF,1: PV event is ON; bit9
    /// </summary>
    public bool PV_EV8 { get; set; } = false;
    public void SetValue(short data)
    {
        PV_EV1 = (data >> 0 & 0x01) == 1;
        PV_EV2 = (data >> 1 & 0x01) == 1;
        PV_EV3 = (data >> 2 & 0x01) == 1;
        PV_EV4 = (data >> 4 & 0x01) == 1;
        PV_EV5 = (data >> 5 & 0x01) == 1;
        PV_EV6 = (data >> 6 & 0x01) == 1;
        PV_EV7 = (data >> 8 & 0x01) == 1;
        PV_EV8 = (data >> 9 & 0x01) == 1;
    }
    public void SetValueSPSeries(short data)
    {
        PV_EV1 = (data >> 0 & 0x01) == 1;
        PV_EV2 = (data >> 1 & 0x01) == 1;
    }
}
/// <summary>
/// 2040
/// </summary>
public class UP55ATimeEventStatus
{
    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit0
    /// </summary>
    public bool Time_EV1 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit1
    /// </summary>
    public bool Time_EV2 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit2
    /// </summary>
    public bool Time_EV3 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit4
    /// </summary>
    public bool Time_EV4 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit5
    /// </summary>
    public bool Time_EV5 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit6
    /// </summary>
    public bool Time_EV6 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit8
    /// </summary>
    public bool Time_EV7 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit9
    /// </summary>
    public bool Time_EV8 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit0-2
    /// </summary>
    public bool Time_EV9 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit1-2
    /// </summary>
    public bool Time_EV10 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit2-2
    /// </summary>
    public bool Time_EV11 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit4-2
    /// </summary>
    public bool Time_EV12 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit5-2
    /// </summary>
    public bool Time_EV13 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit6-2
    /// </summary>
    public bool Time_EV14 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit8-2
    /// </summary>
    public bool Time_EV15 { get; set; } = false;

    /// <summary>
    /// 0: Time event is OFF,1: PV event is ON; bit9-2
    /// </summary>
    public bool Time_EV16 { get; set; } = false;

    public void SetValue(short data1, short data2)
    {
        Time_EV1 = (data1 >> 0 & 0x01) == 1;
        Time_EV2 = (data1 >> 1 & 0x01) == 1;
        Time_EV3 = (data1 >> 2 & 0x01) == 1;
        Time_EV4 = (data1 >> 4 & 0x01) == 1;
        Time_EV5 = (data1 >> 5 & 0x01) == 1;
        Time_EV6 = (data1 >> 6 & 0x01) == 1;
        Time_EV7 = (data1 >> 8 & 0x01) == 1;
        Time_EV8 = (data1 >> 9 & 0x01) == 1;
        Time_EV9 = (data2 >> 0 & 0x01) == 1;
        Time_EV10 = (data2 >> 1 & 0x01) == 1;
        Time_EV11 = (data2 >> 2 & 0x01) == 1;
        Time_EV12 = (data2 >> 4 & 0x01) == 1;
        Time_EV13 = (data2 >> 5 & 0x01) == 1;
        Time_EV14 = (data2 >> 6 & 0x01) == 1;
        Time_EV15 = (data2 >> 8 & 0x01) == 1;
        Time_EV16 = (data2 >> 9 & 0x01) == 1;
    }

    public void SetValueSPSeries(short data)
    {
        Time_EV1 = (data >> 2 & 0x01) == 1;
        Time_EV2 = (data >> 9 & 0x01) == 1;
    }

}
public class UP550ADConverterErrorStatus
{
    /// <summary>
    /// PV1 A/D converter error //bit0
    /// </summary>
    public bool PV1ADC { get; set; } = false;

    /// <summary>
    /// PV1 burnout error//bit1
    /// </summary>
    public bool PV1BO { get; set; } = false;

    /// <summary>
    /// PV1 RJC error//bit2
    /// </summary>
    public bool RJC1ERR { get; set; } = false;

    /// <summary>
    ///PV1 over-scale//bit4
    /// </summary>
    public bool PV1OVERSCALE { get; set; } = false;

    /// <summary>
    /// PV1 under-scale//bit5
    /// </summary>
    public bool PV1UNDERSCALE { get; set; } = false;

    /// <summary>
    /// RSP1 A/D converter error//bit8
    /// </summary>
    public bool RSP1ADC { get; set; } = false;

    /// <summary>
    /// RSP1 burnout error//bit9
    /// </summary>
    public bool RSP1BO { get; set; } = false;

    /// <summary>
    /// RSP1 A/D converter error when RSP1 is used for control//bit12
    /// </summary>
    public bool C_RSP1ADC { get; set; } = false;


    /// <summary>
    /// Burnout error when RSP1 is used for control//bit13
    /// </summary>
    public bool C_RSP1BO { get; set; } = false;

    /// <summary>
    /// Auto-tuning error//bit14
    /// </summary>
    public bool AT1ERR { get; set; } = false;

    public void SetValue(short data)
    {
        PV1ADC = (data >> 0 & 0x01) == 1;
        PV1BO = (data >> 1 & 0x01) == 1;
        RJC1ERR = (data >> 2 & 0x01) == 1;
        PV1OVERSCALE = (data >> 4 & 0x01) == 1;
        PV1UNDERSCALE = (data >> 5 & 0x01) == 1;
        RSP1ADC = (data >> 8 & 0x01) == 1;
        RSP1BO = (data >> 9 & 0x01) == 1;
        C_RSP1ADC = (data >> 12 & 0x01) == 1;
        C_RSP1BO = (data >> 13 & 0x01) == 1;
        AT1ERR = (data >> 14 & 0x01) == 1;
    }
}


