using VsFoundation.Controller.O2.O2Controller.Common;
using VSLibrary.Communication;

namespace VsFoundation.Controller.O2.O2Controller.ZR5.Services;

public class ZR5Client
{
    IO2ConnectionManage _connectionManage;
    ICommunication _connection;
    public bool IsOpen => _connection.IsOpen;
    public ZR5Client(ICommunication connection, CancellationToken _cancellationToken, int timeoutMs = 3000, int retryCount = 3)
    {
        _connection = connection;
        _connectionManage = new O2ConnectionManage(connection, _cancellationToken, timeoutMs, retryCount);
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
    public async Task<bool> Init()
    {
        try
        {
            return await _connectionManage.WriteData("VV\r", "VV", "\r");
        }
        catch
        {
            return false;
        }
    }
    /// <summary>
    /// ppm
    /// </summary>
    /// <returns></returns>
    public async Task<float?> GetOxygen()
    {
        try
        {
            float result;
            var data = await _connectionManage.ReadData("M2\r", "M2", "\r");
            if (ZR5Helper.Parse(eZR5Command.Oxygen, data, out result)) return result;

        }
        catch { }
        return null;
    }
    /// <summary>
    /// mV
    /// </summary>
    /// <returns></returns>
    public async Task<float?> GetCellVoltage()
    {
        try
        {
            float result;
            var data = await _connectionManage.ReadData("A1\r", "A1", "\r");
            if (ZR5Helper.Parse(eZR5Command.CellVoltage, data, out result)) return result;

        }
        catch { }
        return null;
    }
    /// <summary>
    /// °C
    /// </summary>
    /// <returns></returns>
    public async Task<float?> GetTemperature()
    {
        try
        {
            float result;
            var data = await _connectionManage.ReadData("A2\r", "A2", "\r");
            if (ZR5Helper.Parse(eZR5Command.Temperature, data, out result)) return result;
        }
        catch { }
        return null;
    }

}
