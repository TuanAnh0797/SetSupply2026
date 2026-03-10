using VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonType;
using VsFoundation.Controller.TempLimit.TempLimitController.Common.Communication;
using VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Models;
using VSLibrary.Communication;

namespace VsFoundation.Controller.TempLimit.TempLimitController.STSeries.Services;

public class STSeriesClient
{
    ConnectionManage _connectionManage;
    ICommunication _connection;
    public bool IsOpen => _connection.IsOpen;
    public STSeriesClient(ICommunication connection, CancellationToken _cancellationToken, eTempLimitProtocolType protocolType = eTempLimitProtocolType.ModBus, int timeoutMs = 3000, int retryCount = 3)
    {
        IProtocolFrame protocolFrame;
        switch (protocolType)
        {
            case eTempLimitProtocolType.ModBus: protocolFrame = new ModbusRTUFrame(); break;
            case eTempLimitProtocolType.PCLinkSTSeries: protocolFrame = new PCLinkSTSeriesFrame(); break;
            default: throw new ArgumentException($"Not Support '{protocolType}' Frame");
        }
        _connection = connection;
        _connectionManage = new ConnectionManage(protocolFrame, connection, _cancellationToken, timeoutMs, retryCount);
    }
    public async Task<bool> Open()
    {
        try
        {
            var task = Task.Run(() =>
            {
                _connection.OpenAsync();
                return _connection.IsOpen;
            });

            return await task;
        }
        catch
        {
            return false;
        }
    }
    public async Task Close()
    {
        try
        {
            var task = Task.Run(() =>
            {
                _connection.CloseAsync();
            });

            await task;
        }
        catch
        {
        }
    }
    #region Setup_input
    public async Task<bool> SetInputType(byte slaveID, eSTSeriesInputType type)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 600, new short[] { (short)type });
        }
        catch { return false; }
    }
    public async Task<bool> SetInputUnit(byte slaveID, eSTSeriesInputUnit unit)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 601, new short[] { (short)unit });
        }
        catch { return false; }
    }
    public async Task<bool> SetInputHighRange(byte slaveID, float highRange)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 602, new short[] { (short)(highRange * 10) });
        }
        catch { return false; }
    }
    public async Task<bool> SetInputLowRange(byte slaveID, float lowRange)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 603, new short[] { (short)(lowRange * 10) });
        }
        catch { return false; }
    }
    public async Task<bool> SetInputBias(byte slaveID, float bias)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 604, new short[] { (short)(bias * 10) });
        }
        catch { return false; }
    }
    public async Task<STSeriesInput?> GetInputConfig(byte slaveID)
    {

        try
        {
            var listData = await _connectionManage.ReadData(slaveID, 600, 21);
            if (STSeriesHelper.ParseInputConfig(listData, out var outData)) return outData;
            else return null;
        }
        catch { return null; }

    }

    #endregion


    #region Monitor
    public async Task<STSeriesMonitorResult?> MonitorData(byte slaveID)
    {
        try
        {
            var listData = await _connectionManage.ReadData(slaveID, 0, 30);
            if (STSeriesHelper.ParseMonitorData(listData, out var outData)) return outData;
            else return null;
        }
        catch { return null; }
    }
    #endregion

    #region Alarm
    public async Task<bool> SetAlarmType(byte slaveID, STSeriesAlarmType type)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 400, type.ToList().ToArray());
        }
        catch { return false; }
    }
    public async Task<bool> SetAlarmValue(byte slaveID, STSeriesAlarmValue value)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 405, value.ToList().ToArray());
        }
        catch { return false; }
    }
    public async Task<bool> SetAlarmDeadBand(byte slaveID, STSeriesAlarmDeadBand db)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 410, db.ToList().ToArray());
        }
        catch { return false; }
    }
    public async Task<bool> SetAlarmDelayTime(byte slaveID, STSeriesAlarmDelayTime delayTime)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 415, delayTime.ToList().ToArray());
        }
        catch { return false; }
    }
    public async Task<bool> SetAlarmUpperLowerDeviation(byte slaveID, STSeriesAlarmUpperLowerDeviation deviation)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 420, deviation.ToList().ToArray());
        }
        catch { return false; }
    }
    public async Task<bool> SetAlarmHighLowDeviation(byte slaveID, STSeriesAlarmHighLowDeviation deviation)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 439, deviation.ToList().ToArray());
        }
        catch { return false; }
    }
    public async Task<bool> SetAlarmMode(byte slaveID, STSeriesAlarmMode mode)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 453, mode.ToList().ToArray());
        }
        catch { return false; }
    }

    public async Task<STSeriesAlarm?> GetVsAlarmConfig(byte slaveID)
    {
        try
        {
            var listData = await _connectionManage.ReadData(slaveID, 400, 47);
            if (STSeriesHelper.ParseVsAlarmConfig(listData, out var outData)) return outData;
            else return null;
        }
        catch { return null; }
    }
    #endregion


    #region Setpoint group

    public async Task<bool> SetSetPointConfig(byte slaveID, STSeriesSetPoint setpoint)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 199, setpoint.ToList().ToArray());
        }
        catch { return false; }
    }
    public async Task<STSeriesSetPoint?> GetSetPointConfig(byte slaveID)
    {
        try
        {
            var listData = await _connectionManage.ReadData(slaveID, 199, 5);
            if (STSeriesHelper.ParseGetSetPointConfig(listData, out var outData)) return outData;
            else return null;
        }
        catch { return null; }
    }

    #endregion

    #region Output config
    public async Task<bool> SetOutputConfig(byte slaveID, STSeriesOutput output)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 623, output.ToList().ToArray());
        }
        catch { return false; }
    }
    public async Task<STSeriesOutput?> GetOutputConfig(byte slaveID)
    {
        try
        {
            var listData = await _connectionManage.ReadData(slaveID, 623, 5);
            if (STSeriesHelper.ParseGetOutputConfig(listData, out var outData)) return outData;
            else return null;
        }
        catch { return null; }
    }

    #endregion

    #region operation
    public async Task<bool> SetOperationRun(byte slaveID)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 100, new short[] { (short)(0) });
        }
        catch { return false; }
    }
    public async Task<bool> SetOperationStop(byte slaveID)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 100, new short[] { (short)(1) });
        }
        catch { return false; }
    }
    public async Task<bool> SetOperationAuto(byte slaveID)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 104, new short[] { (short)(0) });
        }
        catch { return false; }
    }
    public async Task<bool> SetOperationManual(byte slaveID)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 104, new short[] { (short)(1) });
        }
        catch { return false; }
    }

    #endregion
}