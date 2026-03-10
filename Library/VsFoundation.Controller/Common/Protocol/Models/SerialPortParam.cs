

using System.IO.Ports;

namespace VsFoundation.Controller.Common.Protocol.Models;

public class SerialPortParam
{
    public string Id;
    public string Com;
    public int BaudRate;
    public Parity Parity;
    public int DataBit;
    public StopBits StopBits;
}
