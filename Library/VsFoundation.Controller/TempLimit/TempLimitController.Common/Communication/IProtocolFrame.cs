using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.TempLimit.TempLimitController.Common.Communication;

public interface IProtocolFrame
{
    byte[] CreateRead(byte slaveId, ushort startAddress, ushort numRegisters);
    byte[] CreateWrite(byte slaveId, ushort startAddress, short[] values);
    List<short> GetListReceiveData(byte[] Arr);
    bool ValidByteReceive(byte[] Arr, out string err);        
}
