using VsFoundation.Controller.O2.O2Controller.Common;
using VSLibrary.Communication;

namespace VsFoundation.Controller.O2.O2Controller.ZR5.Services;

public class ZR5Control : IO2Controller
{
    ZR5Client client;
    public bool IsOpen => client.IsOpen;
    public ZR5Control(ICommunication connection, CancellationToken _cancellationToken, int timeoutMs = 3000, int retryCount = 3)
    {
        client = new(connection, _cancellationToken, timeoutMs, retryCount);
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
        return new O2Result() { Oxygen = ret };
    }
    public async Task<bool> Init()
    {
        return await client.Init();
    }
}
