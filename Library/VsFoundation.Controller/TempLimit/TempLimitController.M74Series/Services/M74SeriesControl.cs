using System.Windows.Input;
using System.Windows.Navigation;
using VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonModel;
using VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonType;
using VsFoundation.Controller.TempLimit.TempLimitController.M74Series.Models;
using VSLibrary.Communication;

namespace VsFoundation.Controller.TempLimit.TempLimitController.M74Series.Services;

public class M74SeriesControl : ITempLimit
{
    M74SeriesClient client;
    eTempLimitChannel _channel;
    public bool IsOpen => client.IsOpen;
    public M74SeriesControl(ICommunication connection, CancellationToken _cancellationToken, eTempLimitProtocolType protocolType = eTempLimitProtocolType.ModBus, int timeoutMs = 3000, int retryCount = 3, eTempLimitChannel channel = eTempLimitChannel.CH1)
    {
        client = new(connection, _cancellationToken, protocolType, timeoutMs, retryCount);
        _channel = channel;
    }
    public async Task Close()
    {
        await client.Close();
    }
    public async Task<TempLimitMonitorResult?> MonitorData(byte slaveID)
    {
        var ret = new TempLimitMonitorResult();
        var data = await client.MonitorData(slaveID);
        var channelResult = new M74SeriesChannelMonitorResult();
        if (data == null) return null;
        //
        switch (_channel)
        {
            case eTempLimitChannel.CH1: { channelResult = data.Channel1; } break;
            case eTempLimitChannel.CH2: { channelResult = data.Channel2; } break;
            case eTempLimitChannel.CH3: { channelResult = data.Channel3; } break;
            case eTempLimitChannel.CH4: { channelResult = data.Channel4; } break;
        }
        ret.PV = channelResult.PV;
        ret.SV = channelResult.SV;
        ret.AlarmStatus.IsAlarm1 = channelResult.AlarmStatus.IsAlarm1;
        ret.AlarmStatus.IsAlarm2 = channelResult.AlarmStatus.IsAlarm2;
        ret.AlarmStatus.IsAlarm3 = channelResult.AlarmStatus.IsAlarm3;
        ret.AlarmStatus.IsAlarm4 = channelResult.AlarmStatus.IsAlarm4;
        //
        return ret;
    }
    public async Task<bool> Open()
    {
        return await client.Open();
    }

    public async Task<bool> SetAlarm(byte slaveID, List<TempLimitAlarm> alarms)
    {
        if (alarms.Count == 0) throw new ArgumentException("Alarms is empty");
        if (alarms.Count > 4) throw new ArgumentException("M74 Series only supports 4 alarms");
        //
        var data = new M74SeriesALMGroup();
        for (int i = 0; i < alarms.Count; i++)
        {
            VM74SeriesAlarm tmp = new();
            //
            switch (alarms[i].AlarmType)
            {
                case eTempLimitAlarmType.Off: tmp.AlarmType = eM74SeriesAlarmType.Off; break;
                case eTempLimitAlarmType.AbsoluteUpper: tmp.AlarmType = eM74SeriesAlarmType.AbsoluteUpperAH; break;
                case eTempLimitAlarmType.AbsoluteLower: tmp.AlarmType = eM74SeriesAlarmType.AbsoluteLowerAH; break;
                case eTempLimitAlarmType.UpperDeviation: tmp.AlarmType = eM74SeriesAlarmType.UpperDeviationAH; break;
                case eTempLimitAlarmType.LowerDeviation: tmp.AlarmType = eM74SeriesAlarmType.LowerDeviationAH; break;
            }
            //
            tmp.AlarmDeadBand = alarms[i].AlarmDeadBand;
            tmp.AlarmSetValue = alarms[i].AlarmSetValue;
            tmp.AlarmOutputPort = alarms[i].AlarmOutputPort;
            if (i == 0) data.Alarm1 = tmp;
            else if (i == 1) data.Alarm2 = tmp;
            else if (i == 2) data.Alarm3 = tmp;
            else data.Alarm4 = tmp;
        }
        return await client.SetALMGroupConfig(slaveID, _channel, data);
    }

    public void SetChanel(eTempLimitChannel channel)
    {
        _channel = channel;
    }

    public async Task<bool> SetInputType(byte slaveID, eTempLimitInputType inputType)
    {
        var data = eM74SeriesInputType.K0;
        switch (inputType)
        {
            case eTempLimitInputType.K: data = eM74SeriesInputType.K0; break;
            case eTempLimitInputType.T: data = eM74SeriesInputType.T; break;
            case eTempLimitInputType.R: data = eM74SeriesInputType.R; break;
            case eTempLimitInputType.B: data = eM74SeriesInputType.B; break;
            case eTempLimitInputType.S: data = eM74SeriesInputType.S; break;
            case eTempLimitInputType.N: data = eM74SeriesInputType.N; break;
            case eTempLimitInputType.J: data = eM74SeriesInputType.J; break;
            case eTempLimitInputType.E: data = eM74SeriesInputType.E; break;
            case eTempLimitInputType.L: data = eM74SeriesInputType.L; break;
            case eTempLimitInputType.JPT100: data = eM74SeriesInputType.JPT100; break;
            case eTempLimitInputType.PT100: data = eM74SeriesInputType.PT100; break;
        }
        return await client.SetChannelInputType(slaveID, _channel, data);
    }

    public async Task<bool> SetInputUnit(byte slaveID, eTempLimitUnit unit)
    {
        var data = eM74SeriesUnit.DegreeC;
        switch (unit)
        {
            case eTempLimitUnit.DegreeC: data = eM74SeriesUnit.DegreeC; break;
            case eTempLimitUnit.DegreeF: data = eM74SeriesUnit.DegreeF; break;

        }
        return await client.SetInputUnit(slaveID, data);
    }
    public async Task<bool> SetInputBias(byte slaveID, float bias)
    {
        await Task.Delay(1);
        return true;
    }
    public async Task<bool> SetSVValue(byte slaveID, float sv)
    {
        //
        var data = new M74SeriesSVGroup();
        data.Number = eM74SeriesSVNumber.SV0;
        data.Value0 = sv;
        return await client.SetSVGroupConfig(slaveID, _channel, data);
    }

    public async Task<bool> Start(byte slaveID)
    {
        return await client.SetChannelStartStop(slaveID, _channel, eM74SeriesChannelStartStop.Run);
    }

    public async Task<bool> Stop(byte slaveID)
    {
        return await client.SetChannelStartStop(slaveID, _channel, eM74SeriesChannelStartStop.Stop);
    }


}
