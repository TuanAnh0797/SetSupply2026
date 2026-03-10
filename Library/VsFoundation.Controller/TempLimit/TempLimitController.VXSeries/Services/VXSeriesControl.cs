using VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonModel;
using VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonType;
using VsFoundation.Controller.TempLimit.TempLimitController.VXSeries.Models;
using VSLibrary.Communication;

namespace VsFoundation.Controller.TempLimit.TempLimitController.VXSeries.Services;

public class VXSeriesControl : ITempLimit
{
    VXSeriesClient client;
    eTempLimitChannel _channel;
    public bool IsOpen => client.IsOpen;
    public VXSeriesControl(ICommunication connection, CancellationToken _cancellationToken, eTempLimitProtocolType protocolType = eTempLimitProtocolType.ModBus, int timeoutMs = 3000, int retryCount = 3, eTempLimitChannel channel = eTempLimitChannel.CH1)
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
        if (data == null) return null;

        ret.PV = data.CPV;
        ret.SV = data.CSV;
        ret.AlarmStatus.IsAlarm1 = data.Alarm1Status == eVXSeriesStatus.ON;
        ret.AlarmStatus.IsAlarm2 = data.Alarm2Status == eVXSeriesStatus.ON;
        ret.AlarmStatus.IsAlarm3 = data.Alarm3Status == eVXSeriesStatus.ON;
        ret.AlarmStatus.IsAlarm4 = data.Alarm4Status == eVXSeriesStatus.ON;
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
        if (alarms.Count > 4) throw new ArgumentException("VX Series only supports 4 alarms");
        //
        var data = new VXSeriesALMGroup();
        for (int i = 0; i < alarms.Count; i++)
        {
            VXSeriesAlarm tmp = new();
            //
            switch (alarms[i].AlarmType)
            {
                case eTempLimitAlarmType.Off: tmp.AlarmType = eVXSeriesAlarmType.AlarmOFF; break;
                case eTempLimitAlarmType.AbsoluteUpper: tmp.AlarmType = eVXSeriesAlarmType.HighAbsolute; break;
                case eTempLimitAlarmType.AbsoluteLower: tmp.AlarmType = eVXSeriesAlarmType.LowAbsolute; break;
                case eTempLimitAlarmType.UpperDeviation: tmp.AlarmType = eVXSeriesAlarmType.HighDeviation; break;
                case eTempLimitAlarmType.LowerDeviation: tmp.AlarmType = eVXSeriesAlarmType.LowDeviation; break;
            }
            //
            tmp.AlarmDeadBand = alarms[i].AlarmDeadBand;
            tmp.AlarmSetValue = alarms[i].AlarmSetValue;
            if (i == 0) data.Alarm1 = tmp;
            else if (i == 1) data.Alarm2 = tmp;
            else if (i == 2) data.Alarm3 = tmp;
            else data.Alarm4 = tmp;
        }
        return await client.SetALMGroupConfig(slaveID, data);
    }

    public void SetChanel(eTempLimitChannel channel)
    {
        _channel = channel;
    }

    public async Task<bool> SetInputType(byte slaveID, eTempLimitInputType inputType)
    {
        var data = eVXSeriesInputType.K0;
        switch (inputType)
        {
            case eTempLimitInputType.K: data = eVXSeriesInputType.K0; break;
            case eTempLimitInputType.T: data = eVXSeriesInputType.T1; break;
            case eTempLimitInputType.R: data = eVXSeriesInputType.R0; break;
            case eTempLimitInputType.B: data = eVXSeriesInputType.B0; break;
            case eTempLimitInputType.S: data = eVXSeriesInputType.S0; break;
            case eTempLimitInputType.N: data = eVXSeriesInputType.N0; break;
            case eTempLimitInputType.J: data = eVXSeriesInputType.JO; break;
            case eTempLimitInputType.E: data = eVXSeriesInputType.E1; break;
            case eTempLimitInputType.L: data = eVXSeriesInputType.L1; break;
            case eTempLimitInputType.JPT100: data = eVXSeriesInputType.JPT0; break;
            case eTempLimitInputType.PT100: data = eVXSeriesInputType.PT0; break;
        }
        return await client.SetInType(slaveID, data);
    }

    public async Task<bool> SetInputUnit(byte slaveID, eTempLimitUnit unit)
    {
        var data = eVXSeriesUnit.DegreeC;
        switch (unit)
        {
            case eTempLimitUnit.DegreeC: data = eVXSeriesUnit.DegreeC; break;
            case eTempLimitUnit.DegreeF: data = eVXSeriesUnit.DegreeF; break;

        }
        return await client.SetInUnit(slaveID, data);
    }
    public async Task<bool> SetInputBias(byte slaveID, float bias)
    {
        return await client.SetInBias(slaveID, (short)bias);
    }
    public async Task<bool> SetSVValue(byte slaveID, float sv)
    {
        //
        var data = new VXSeriesSVGroup();
        data.Number = eVXSeriesSVNumber.SV1;
        data.Value1 = sv;
        return await client.SetSVGroupConfig(slaveID, data);
    }

    public async Task<bool> Start(byte slaveID)
    {
        await Task.Delay(1);
        return true;
    }

    public async Task<bool> Stop(byte slaveID)
    {
        await Task.Delay(1);
        return true;
    }
}
