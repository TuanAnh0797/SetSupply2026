

using System.IO.Ports;

namespace VsFoundation.Controller.Common.Protocol.Interface;

public interface ISerialConnect : IConnectable
{
    public int BaudRate { get; set; }
    public int DataBit { get; set; }
    public Parity Parity { get; set; }
    public StopBits StopBits { get; set; }
}
public interface IConnectable
{
    string DeviceID { get; set; }
    bool IsConnected { get; set; }
    Task<bool> Connect();
    //void Disconnect();
    Task Disconnect();
    public Task<byte[]> SendCommandAndReceiveResponseFullyAsync(byte[] commandBytes, int timeout);
    public CancellationTokenSource CancellationTokenSource { get; set; }
    void DiscardBuffer();
    event Action<string, bool> ConnectionChanged;
    event Action<string, bool> StatusPort;
}
