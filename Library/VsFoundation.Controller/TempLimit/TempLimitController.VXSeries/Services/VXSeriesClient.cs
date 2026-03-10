using VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonType;
using VsFoundation.Controller.TempLimit.TempLimitController.Common.Communication;
using VsFoundation.Controller.TempLimit.TempLimitController.VXSeries.Models;
using VSLibrary.Communication;

namespace VsFoundation.Controller.TempLimit.TempLimitController.VXSeries.Services;

public class VXSeriesClient
{
    ConnectionManage _connectionManage;
    ICommunication _connection;
    public bool IsOpen => _connection.IsOpen;
    public VXSeriesClient(ICommunication connection, CancellationToken _cancellationToken, eTempLimitProtocolType protocolType = eTempLimitProtocolType.ModBus, int timeoutMs = 3000, int retryCount = 3)
    {
        IProtocolFrame protocolFrame;
        switch (protocolType)
        {
            case eTempLimitProtocolType.ModBus: protocolFrame = new ModbusRTUFrame(); break;
            case eTempLimitProtocolType.PCLinkVXSeries: protocolFrame = new PCLinkVXFrame(); break;
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

    #region Main Function
    // Sub group
    public async Task<bool?> SetSUBGroupConfig(byte slaveID, VXSeriesSUBGroup config)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 500, config.ToListByte().ToArray());
        }
        catch
        {
            return null;
        }
    }
    public async Task<VXSeriesSUBGroup?> GetSUBGroupConfig(byte slaveID)
    {
        try
        {
            var listData = await _connectionManage.ReadData(slaveID, 500, 2);
            if (VXSeriesHelper.ParseSUBGroup(listData, out var outData)) return outData;
            else return null;
        }
        catch { return null; }
    }
    // Alarm group
    public async Task<bool> SetALMGroupConfig(byte slaveID, VXSeriesALMGroup config)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 300, config.ToListByte().ToArray());
        }
        catch
        {
            return false;
        }
    }
    public async Task<VXSeriesALMGroup?> GetALMGroupConfig(byte slaveID)
    {
        try
        {
            var listData = await _connectionManage.ReadData(slaveID, 300, 16);
            if (VXSeriesHelper.ParseALMGroup(listData, out var outData)) return outData;
            else return null;
        }
        catch { return null; }
    }
    //Monitor
    public async Task<VXSeriesMonitorResult?> MonitorData(byte slaveID)
    {
        try
        {
            var listData = await _connectionManage.ReadData(slaveID, 0, 24);
            if (VXSeriesHelper.ParseMonitorData(listData, out var outData)) return outData;
            else return null;
        }
        catch { return null; }
    }

    // Input group 
    public async Task<bool> SetInType(byte slaveID, eVXSeriesInputType inputType)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 900, new short[] { (short)inputType });
        }
        catch { return false; }
    }
    public async Task<bool> SetInUnit(byte slaveID, eVXSeriesUnit unit)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 901, new short[] { (short)unit });
        }
        catch { return false; }
    }
    public async Task<bool> SetInRJC(byte slaveID, eVXSeriesStatus rjc)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 907, new short[] { (short)rjc });
        }
        catch { return false; }
    }
    public async Task<bool> SetInBias(byte slaveID, short bias)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 909, new short[] { (short)bias });
        }
        catch { return false; }
    }
    public async Task<VXSeriesINGroup?> GetINGroupConfig(byte slaveID)
    {
        try
        {
            var listData = await _connectionManage.ReadData(slaveID, 900, 10);
            if (VXSeriesHelper.ParseReadINGroup(listData, out var outData)) return outData;
            else return null;
        }
        catch { return null; }
    }

    // Set value group

    public async Task<bool> SetSVGroupConfig(byte slaveID, VXSeriesSVGroup config)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, 100, config.ToListByte().ToArray());
        }
        catch
        {
            return false;
        }
    }
    public async Task<VXSeriesSVGroup?> GetSVGroupConfig(byte slaveID)
    {
        try
        {
            var listData = await _connectionManage.ReadData(slaveID, 100, 7);
            if (VXSeriesHelper.ParseReadSVGroup(listData, out var outData)) return outData;
            else return null;
        }
        catch { return null; }
    }
    #endregion

}
