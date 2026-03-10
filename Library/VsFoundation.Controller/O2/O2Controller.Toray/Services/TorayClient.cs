using MySqlX.XDevAPI;
using VsFoundation.Controller.O2.O2Controller.Common;
using VSLibrary.Communication;

namespace VsFoundation.Controller.O2.O2Controller.Toray.Services;

public class TorayClient
{
    ICommunication _connection;
    public bool IsOpen => _connection.IsOpen;
    O2Result? _o2Result;
    DateTime _ReceiveTime;
    public TorayClient(ICommunication connection, eO2ControllerType type)
    {
        _connection = connection;
        _ReceiveTime = new();
        connection.DataReceived += (object? sender, byte[] e) => { if (TorayHelper.Parse(type, e, out _o2Result)) _ReceiveTime = DateTime.Now; };
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
    public async Task<O2Result?> GetOxygen()
    {
        await Task.Delay(1);
        if (DateTime.Now.AddSeconds(-3) < _ReceiveTime) return null;
        return _o2Result;
    }
}
