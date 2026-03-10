
using VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonType;
using VsFoundation.Controller.TempLimit.TempLimitController.M74Series.Models;

namespace VsFoundation.Controller.TempLimit.TempLimitController.M74Series.Services;

public static class M74SeriesHelper
{

    public static bool ParseReadSVGroup(in List<short> lstData, out M74SeriesSVGroup result)
    {
        result = new();
        int count = 0;
        if (lstData.Count < 5) { return false; }
        result.Number = (eM74SeriesSVNumber)lstData[count++];
        result.Value0 = lstData[count++];
        result.Value1 = lstData[count++];
        result.Value2 = lstData[count++];
        result.Value3 = lstData[count++];
        //
        return true;
    }
    public static bool ParseMonitorData(in List<short> lstData, out M74SeriesMonitorResult result)
    {
        result = new();
        int count = 0;

        if (lstData.Count < 34) { return false; }
        //PV
        result.Channel1.PV = lstData[count++];
        result.Channel2.PV = lstData[count++];
        result.Channel3.PV = lstData[count++];
        result.Channel4.PV = lstData[count++];

        //SV
        result.Channel1.SV = lstData[count++];
        result.Channel2.SV = lstData[count++];
        result.Channel3.SV = lstData[count++];
        result.Channel4.SV = lstData[count++];

        //MV
        result.Channel1.MV = lstData[count++];
        result.Channel2.MV = lstData[count++];
        result.Channel3.MV = lstData[count++];
        result.Channel4.MV = lstData[count++];

        //AlarmStatus
        count = 30;
        result.Channel1.AlarmStatus.SetValue(lstData[count++]);
        result.Channel2.AlarmStatus.SetValue(lstData[count++]);
        result.Channel3.AlarmStatus.SetValue(lstData[count++]);
        result.Channel4.AlarmStatus.SetValue(lstData[count++]);

        return true;
    }
    public static bool ParseALMGroup(in List<short> lstData, out M74SeriesALMGroup result)
    {
        result = new();
        if (lstData.Count < 18) { return false; }
        int count = 0;
        result.RelayControl.SetValueFromByte(lstData[count++]);
        count++;
        //
        result.Alarm1.AlarmType = (eM74SeriesAlarmType)lstData[count++];
        result.Alarm2.AlarmType = (eM74SeriesAlarmType)lstData[count++];
        result.Alarm3.AlarmType = (eM74SeriesAlarmType)lstData[count++];
        result.Alarm4.AlarmType = (eM74SeriesAlarmType)lstData[count++];

        //
        result.Alarm1.AlarmDeadBand = lstData[count++];
        result.Alarm2.AlarmDeadBand = lstData[count++];
        result.Alarm3.AlarmDeadBand = lstData[count++];
        result.Alarm4.AlarmDeadBand = lstData[count++];
        //
        result.Alarm1.AlarmSetValue = lstData[count++];
        result.Alarm2.AlarmSetValue = lstData[count++];
        result.Alarm3.AlarmSetValue = lstData[count++];
        result.Alarm4.AlarmSetValue = lstData[count++];
        //
        result.Alarm1.AlarmOutputPort = (eM74SeriesAlarmOutputPort)lstData[count++];
        result.Alarm2.AlarmOutputPort = (eM74SeriesAlarmOutputPort)lstData[count++];
        result.Alarm3.AlarmOutputPort = (eM74SeriesAlarmOutputPort)lstData[count++];
        result.Alarm4.AlarmOutputPort = (eM74SeriesAlarmOutputPort)lstData[count++];
        return true;
    }
}

public static class M74SeriesAddressManager
{
    public static readonly ushort Monitor = 0;// count 34
    public static readonly ushort Lock = 50;// count 1
    public static readonly ushort Unit = 77;// count 1
    public static ushort ChannelMode(eTempLimitChannel channel)
    {
        return channel switch
        {
            eTempLimitChannel.CH1 => 53,
            eTempLimitChannel.CH2 => 54,
            eTempLimitChannel.CH3 => 55,
            eTempLimitChannel.CH4 => 56,
            _ => throw new InvalidDataException()
        };
    }
    public static ushort ChannelStartStop(eTempLimitChannel channel)
    {
        return channel switch
        {
            eTempLimitChannel.CH1 => 59,
            eTempLimitChannel.CH2 => 60,
            eTempLimitChannel.CH3 => 61,
            eTempLimitChannel.CH4 => 62,
            _ => throw new InvalidDataException()
        };

    }
    public static ushort ChannelInputType(eTempLimitChannel channel)
    {
        return channel switch
        {
            eTempLimitChannel.CH1 => 73,
            eTempLimitChannel.CH2 => 74,
            eTempLimitChannel.CH3 => 75,
            eTempLimitChannel.CH4 => 76,
            _ => throw new InvalidDataException()
        };

    }
    public static ushort ChannelAlarm(eTempLimitChannel channel)
    {
        //count 18
        return channel switch
        {
            eTempLimitChannel.CH1 => 127,
            eTempLimitChannel.CH2 => 227,
            eTempLimitChannel.CH3 => 327,
            eTempLimitChannel.CH4 => 427,
            _ => throw new InvalidDataException()
        };

    }
    public static ushort ChannelSetValue(eTempLimitChannel channel)
    {
        //count 5
        return channel switch
        {
            eTempLimitChannel.CH1 => 116,
            eTempLimitChannel.CH2 => 216,
            eTempLimitChannel.CH3 => 316,
            eTempLimitChannel.CH4 => 416,
            _ => throw new InvalidDataException()
        };

    }
}