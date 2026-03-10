using System.Text;
using VsFoundation.Controller.DIOBoard.DIOBoard.ICPDas.Models;
using VsFoundation.Controller.DIOBoard.DIOBoardController.Common.Communication;
using VSLibrary.Communication;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace VsFoundation.Controller.DIOBoard.DIOBoard.ICPDas.Services;

public class ICPDasClient
{
    ICPDasConnectionManage _connectionManage;
    ICommunication _connection;
    public bool IsOpen => _connection.IsOpen;
    public ICPDasClient(ICommunication connection, CancellationToken _cancellationToken, int timeoutMs = 3000, int retryCount = 3)
    {
        _connection = connection;
        _connectionManage = new ICPDasConnectionManage( connection, _cancellationToken, timeoutMs, retryCount);
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
    public async Task<bool> SetOutDO7045(byte slaveID, ICPDasDO7045 config)
    {
        try
        {
            var dataSend = Encoding.ASCII.GetBytes($"@{slaveID:D2}00{config.GetOutByteToSent()[0].ToString("X2")}\r");
            if (!await _connectionManage.WriteData(dataSend)) return false;
            dataSend = Encoding.ASCII.GetBytes($"@{slaveID:D2}0B{config.GetOutByteToSent()[1].ToString("X2")}\r");
            if (!await _connectionManage.WriteData(dataSend)) return false;
            return true;
        }
        catch
        {
            return false;
        }
    }
    public async Task<bool> SetOutDIO7055(byte slaveID, ICPDasDIO7055 config)
    {
        try
        {
            var dataSend = Encoding.ASCII.GetBytes($"@{slaveID:D2}0B{config.GetOutByteToSent().ToString("X2")}\r");
            return await _connectionManage.WriteData(dataSend);
        }
        catch
        {
            return false;
        }
    }
    public async Task<ICPDasDIO7055?> GetDIO7055Status(byte slaveID)
    {
        try
        {
            var dataSend = Encoding.ASCII.GetBytes($"@{slaveID:D2}\r");
            var data = await _connectionManage.ReadData(dataSend);
            if (ICPDasHelper.ParseReadDIO7055(data, out var outData)) { return outData; }
            return null;
        }
        catch
        {
            return null;
        }
    }
    public async Task<ICPDasDO7045?> GetDO7045Status(byte slaveID)
    {
        try
        {
            var dataSend = Encoding.ASCII.GetBytes($"@{slaveID:D2}\r");
            var data = await _connectionManage.ReadData(dataSend);
            if (ICPDasHelper.ParseReadDO7045(data, out var outData)) { return outData; }
            return null;
        }
        catch
        {
            return null;
        }
    }
    public async Task<ICPDasDI7051?> GetDI7051Status(byte slaveID)
    {
        try
        {
            var dataSend = Encoding.ASCII.GetBytes($"@{slaveID:D2}\r");
            var data = await _connectionManage.ReadData(dataSend);
            if (ICPDasHelper.ParseReadDI7051(data, out var outData)) { return outData; }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
