
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Common.Protocol.Enum;

namespace VsFoundation.Controller.Common.Protocol.Interface;

public interface IProtocol
{
    byte[] CreateRead(byte slaveId, ushort startAddress, ushort numRegisters);
    byte[] CreateWrite(byte slaveId, ushort startAddress, short value);
    byte[] CreateWriteMultiple(byte slaveId, ushort startAddress, short[] values);
    byte[] CreateWriteSingleCoil(byte slaveId, ushort startAddress, eStatusCoil value);
    public byte[] CreateReadCoil(byte slaveId, ushort startAddress, ushort numRegisters);
    List<short> GetListReceiveData(byte[] Arr);
    List<short> GetListReceiveDataCoil(byte[] Arr);
    bool ValidByteReceive(byte[] Arr, out string err);
    public bool ParsesWriteSingleRegister(byte[] dataParse);
    public short ParsesReadSingleRegister(byte[] dataParse);
}
