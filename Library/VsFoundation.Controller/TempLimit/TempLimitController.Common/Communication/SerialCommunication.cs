using System.IO.Ports;
using VSLibrary.Communication.Serial;

namespace VsFoundation.Controller.TempLimit.TempLimitController.Common.Communication;

public class SerialCommunication : SerialBase
{
    public SerialCommunication(string portName, int baudRate, Parity parity=Parity.None, int dataBits=8, StopBits stopBits=StopBits.One) : base(portName, baudRate, parity, dataBits, stopBits)
    {

    }
}
