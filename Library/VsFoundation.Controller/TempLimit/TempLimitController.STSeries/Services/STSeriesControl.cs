using VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonModel;
using VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonType;
using VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Models;
using VSLibrary.Communication;

namespace VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Services;

public class STSeriesControl : ITempLimit
{
    STSeriesClient client;
    eTempLimitChannel _channel;
    public bool IsOpen => client.IsOpen;
    public STSeriesControl(ICommunication connection, CancellationToken _cancellationToken, eTempLimitProtocolType protocolType = eTempLimitProtocolType.ModBus, int timeoutMs = 3000, int retryCount = 3, eTempLimitChannel channel = eTempLimitChannel.CH1)
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

        ret.PV = data.NPV;
        ret.SV = data.NSP;
        ret.AlarmStatus.IsAlarm1 = data.AlarmStatus.ALM1;
        ret.AlarmStatus.IsAlarm2 = data.AlarmStatus.ALM2;
        ret.AlarmStatus.IsAlarm3 = data.AlarmStatus.ALM3;
        ret.AlarmStatus.IsAlarm4 = data.AlarmStatus.ALM4;
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
        if (alarms.Count > 4) throw new ArgumentException("ST Series only supports 4 alarms");
        //
        var alarmType = new STSeriesAlarmType();
        var alarmValue = new STSeriesAlarmValue();
        var alarmDeadBand = new STSeriesAlarmDeadBand();

        for (int i = 0; i < alarms.Count; i++)
        {
            //
            eSTSeriesAlarmType type = eSTSeriesAlarmType.OFF;
            switch (alarms[i].AlarmType)
            {
                case eTempLimitAlarmType.Off: type = eSTSeriesAlarmType.OFF; break;
                case eTempLimitAlarmType.AbsoluteUpper: type = eSTSeriesAlarmType.UpperOfPV; break;
                case eTempLimitAlarmType.AbsoluteLower: type = eSTSeriesAlarmType.LowerOfPV; break;
                case eTempLimitAlarmType.UpperDeviation: type = eSTSeriesAlarmType.UpperOfDeviation; break;
                case eTempLimitAlarmType.LowerDeviation: type = eSTSeriesAlarmType.LowerOfDeviation; break;
            }
            //
            if (i == 0)
            {
                alarmType.Alarm1 = type;
                alarmValue.Alarm1 = alarms[i].AlarmSetValue;
                alarmDeadBand.Alarm1 = alarms[i].AlarmDeadBand;
            }
            else if (i == 1)
            {
                alarmType.Alarm2 = type;
                alarmValue.Alarm2 = alarms[i].AlarmSetValue;
                alarmDeadBand.Alarm2 = alarms[i].AlarmDeadBand;


            }
            else if (i == 2)
            {
                alarmType.Alarm3 = type;
                alarmValue.Alarm3 = alarms[i].AlarmSetValue;
                alarmDeadBand.Alarm3 = alarms[i].AlarmDeadBand;

            }
            else
            {
                alarmType.Alarm4 = type;
                alarmValue.Alarm4 = alarms[i].AlarmSetValue;
                alarmDeadBand.Alarm4 = alarms[i].AlarmDeadBand;

            }
        }
        if (!await client.SetAlarmDeadBand(slaveID, alarmDeadBand)) return false;
        if (!await client.SetAlarmType(slaveID, alarmType)) return false;
        if (!await client.SetAlarmValue(slaveID, alarmValue)) return false;
        return true;
    }

    public void SetChanel(eTempLimitChannel channel)
    {
        _channel = channel;
    }

    public async Task<bool> SetInputType(byte slaveID, eTempLimitInputType inputType)
    {
        var data = eSTSeriesInputType.K1;
        switch (inputType)
        {
            case eTempLimitInputType.K: data = eSTSeriesInputType.K1; break;
            case eTempLimitInputType.T: data = eSTSeriesInputType.T; break;
            case eTempLimitInputType.R: data = eSTSeriesInputType.R; break;
            case eTempLimitInputType.B: data = eSTSeriesInputType.B; break;
            case eTempLimitInputType.S: data = eSTSeriesInputType.S; break;
            case eTempLimitInputType.N: data = eSTSeriesInputType.N; break;
            case eTempLimitInputType.J: data = eSTSeriesInputType.J; break;
            case eTempLimitInputType.E: data = eSTSeriesInputType.E; break;
            case eTempLimitInputType.L: data = eSTSeriesInputType.L; break;
            case eTempLimitInputType.JPT100: data = eSTSeriesInputType.JPTA; break;
            case eTempLimitInputType.PT100: data = eSTSeriesInputType.PTA; break;
        }
        return await client.SetInputType(slaveID, data);
    }

    public async Task<bool> SetInputUnit(byte slaveID, eTempLimitUnit unit)
    {
        var data = eSTSeriesInputUnit.C;
        switch (unit)
        {
            case eTempLimitUnit.DegreeC: data = eSTSeriesInputUnit.C; break;
            case eTempLimitUnit.DegreeF: data = eSTSeriesInputUnit.F; break;

        }
        return await client.SetInputUnit(slaveID, data);
    }
    public async Task<bool> SetInputBias(byte slaveID, float bias)
    {
        return await client.SetInputBias(slaveID, (short)bias);
    }
    public async Task<bool> SetSVValue(byte slaveID, float sv)
    {
        //
        var data = new STSeriesSetPoint();
        data.SetpointType = eSTSeriesSetpointType.SP1;
        data.Setpoint1 = sv;
        return await client.SetSetPointConfig(slaveID, data);
    }
    public async Task<bool> Start(byte slaveID)
    {
        return await client.SetOperationRun(slaveID);
    }

    public async Task<bool> Stop(byte slaveID)
    {
        return await client.SetOperationStop(slaveID);
    }
}

