using System.IO.Ports;

namespace VsFoundation.Controller.Common.Protocol.Interface;

public interface ICommunicationHandler
{
    public bool OpenConnection();
    public bool CloseConnection();
    public Task<byte[]> SendCommandAndReceiveResponseFullyAsync(byte[] commandBytes, int timeout);
    public Task<byte[]> ReceiveResponseAsync(int timeout);


}