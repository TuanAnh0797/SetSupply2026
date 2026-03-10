
using VsFoundation.Controller.O2.O2Controller.Common;
using VSLibrary.Communication;

namespace VsFoundation.Controller.O2.O2Controller.Toray.Services;

public class TorayControl : IO2Controller
{
    TorayClient client;
    public bool IsOpen => client.IsOpen;
    public TorayControl(ICommunication connection, eO2ControllerType type)
    {
        client = new(connection, type);
    }
    public async Task Close()
    {
        await client.Close();
    }
    public async Task<bool> Open()
    {
        return await client.Open();
    }
    public async Task<O2Result?> GetOxygen()
    {
        var ret = await client.GetOxygen();
        if (ret == null) return null;
        return ret;
    }
    public async Task<bool> Init()
    {
        await Task.Delay(1);
        return true;
    }
}