using VsFoundation.Controller.Common.Protocol.Enum;
using VsFoundation.Controller.DIOBoard.DIOBoard.OvenDIOBoard.Models;
using VsFoundation.Controller.DIOBoard.DIOBoardController.Common.Communication;
using VSLibrary.Communication;

namespace VsFoundation.Controller.DIOBoard.DIOBoard.OvenDIOBoard.Services;

public class OvenDIOBoardClient
{
    ConnectionManage _connectionManage;
    ICommunication _connection;
    public bool IsOpen => _connection.IsOpen;
    public OvenDIOBoardClient(ICommunication connection, CancellationToken _cancellationToken, int timeoutMs = 3000, int retryCount = 3)
    {
        IProtocolFrame protocolFrame;
        protocolFrame = new ModbusRTUFrame();
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
    public async Task<bool> SetOutputState(byte slaveID, eOutputCoil type, eStatusCoil state)
    {
        try
        {
            return await _connectionManage.WriteData(slaveID, (ushort)type, state);
        }
        catch
        {
            return false;
        }
    }
    public async Task<DIOState?> MonitorData(byte slaveID)
    {
        try
        {
            var data = await _connectionManage.ReadData(slaveID, 0, 96);
            if (OvenDIOBoardHelper.ParseMonitorData(data, out var dIOState)) { return dIOState; }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
