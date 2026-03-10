
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Common.Protocol.Enum;
using VsFoundation.Controller.Common.Protocol.Interface;

namespace VsFoundation.Controller.Common.Protocol.Serial.LoggerTempController;

public class DconI7018 : IProtocol
{
    public byte[] CreateRead(byte slaveId, ushort startAddress, ushort numRegisters)
    {
        throw new NotImplementedException();
    }

    public byte[] CreateReadCoil(byte slaveId, ushort startAddress, ushort numRegisters)
    {
        throw new NotImplementedException();
    }

    public byte[] CreateWrite(byte slaveId, ushort startAddress, short value)
    {
        throw new NotImplementedException();
    }

    public byte[] CreateWriteMultiple(byte slaveId, ushort startAddress, short[] values)
    {
        throw new NotImplementedException();
    }

    public byte[] CreateWriteSingleCoil(byte slaveId, ushort startAddress, eStatusCoil value)
    {
        throw new NotImplementedException();
    }

    public List<short> GetListReceiveData(byte[] Arr)
    {
        throw new NotImplementedException();
    }

    public List<short> GetListReceiveDataCoil(byte[] Arr)
    {
        throw new NotImplementedException();
    }

    public short ParsesReadSingleRegister(byte[] dataParse)
    {
        throw new NotImplementedException();
    }

    public bool ParsesWriteSingleRegister(byte[] dataParse)
    {
        throw new NotImplementedException();
    }

    public bool ValidByteReceive(byte[] Arr, out string err)
    {
        err = string.Empty;
        if (Arr[Arr.Length - 1] == 13)
        {
            return true;
        }
        else
        {
            err = "Not End";
            return false;
        }
    }
}
