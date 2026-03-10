using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using VsFoundation.Controller.TempLimit.TempLimitController.Common.CommonType;
using VsFoundation.Controller.TempLimit.TempLimitController.Common.Communication;
using VsFoundation.Controller.TempLimit.TempLimitController.M74Series.Models;
using VSLibrary.Communication;

namespace VsFoundation.Controller.TempLimit.TempLimitController.M74Series.Services;

public class M74SeriesClient
{
    ConnectionManage _connectionManage;
    ICommunication _connection;
    public bool IsOpen => _connection.IsOpen;
    public M74SeriesClient(ICommunication connection, CancellationToken _cancellationToken, eTempLimitProtocolType protocolType = eTempLimitProtocolType.ModBus, int timeoutMs = 3000, int retryCount = 3)
    {
        IProtocolFrame protocolFrame;
        switch (protocolType)
        {
            case eTempLimitProtocolType.ModBus: protocolFrame = new ModbusRTUFrame(); break;
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
                _connection.OpenAsync();
            });

            await task;
        }
        catch
        {
        }
    }
    #region Main Function
    
    // Alarm group
    public async Task<bool> SetALMGroupConfig(byte slaveID, eTempLimitChannel channel, M74SeriesALMGroup config)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, M74SeriesAddressManager.ChannelAlarm(channel), config.ToListByte().ToArray());
        }
        catch
        {
            return false;
        }
    }
    public async Task<M74SeriesALMGroup?> GetALMGroupConfig(byte slaveID, eTempLimitChannel channel)
    {
        try
        {
            var listData = await _connectionManage.ReadData(slaveID, M74SeriesAddressManager.ChannelAlarm(channel), 18);
            if (M74SeriesHelper.ParseALMGroup(listData, out var outData)) return outData;
            else return null;
        }
        catch { return null; }
    }
    //Monitor
    public async Task<M74SeriesMonitorResult?> MonitorData(byte slaveID)
    {
        try
        {
            var listData = await _connectionManage.ReadData(slaveID, M74SeriesAddressManager.Monitor, 34);
            if (M74SeriesHelper.ParseMonitorData(listData, out var outData)) return outData;
            else return null;
        }
        catch { return null; }
    }


    // Set value group

    public async Task<bool> SetSVGroupConfig(byte slaveID, eTempLimitChannel channel,M74SeriesSVGroup config)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, M74SeriesAddressManager.ChannelSetValue(channel), config.ToListByte().ToArray());
        }
        catch
        {
            return false;
        }
    }
    public async Task<M74SeriesSVGroup?> GetSVGroupConfig(byte slaveID, eTempLimitChannel channel)
    {
        try
        {
            var listData = await _connectionManage.ReadData(slaveID, M74SeriesAddressManager.ChannelSetValue(channel), 5);
            if (M74SeriesHelper.ParseReadSVGroup(listData, out var outData)) return outData;
            else return null;
        }
        catch { return null; }
    }
    #endregion
    public async Task<bool> SetLockStatus(byte slaveID, eM74SeriesLockStatus lockStatus)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, M74SeriesAddressManager.Lock, new short[] { (short)lockStatus });
        }
        catch { return false; }
    }
    public async Task<bool> SetChannelMode(byte slaveID, eTempLimitChannel channel, eM74SeriesChannelMode channelMode)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, M74SeriesAddressManager.ChannelMode(channel), new short[] { (short)channelMode });
        }
        catch { return false; }
    }
    public async Task<bool> SetChannelStartStop(byte slaveID, eTempLimitChannel channel, eM74SeriesChannelStartStop status)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, M74SeriesAddressManager.ChannelStartStop(channel), new short[] { (short)status });
        }
        catch { return false; }
    }
    public async Task<bool> SetChannelInputType(byte slaveID, eTempLimitChannel channel, eM74SeriesInputType type)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, M74SeriesAddressManager.ChannelInputType(channel), new short[] { (short)type });
        }
        catch { return false; }
    }
    public async Task<bool> SetInputUnit(byte slaveID, eM74SeriesUnit unit)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, M74SeriesAddressManager.Unit, new short[] { (short)unit });
        }
        catch { return false; }
    }
}
