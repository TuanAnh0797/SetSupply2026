using VsFoundation.Controller.Common.Protocol.Enum;

namespace VsFoundation.Controller.DIOBoard.DIOBoardController.Common.Communication;

public interface IProtocolFrame
{
    byte[] CreateRead(byte slaveId, ushort startAddress, ushort numRegisters);
    byte[] CreateReadCoil(byte slaveId, ushort startAddress, ushort numRegisters);
    byte[] CreateWrite(byte slaveId, ushort startAddress, short[] values);
    byte[] CreateWriteSingleCoil(byte slaveId, ushort startAddress, eStatusCoil value);
    List<short> GetListReceiveData(byte[] Arr);
    bool ValidByteReceive(byte[] Arr, out string err);
    List<short> GetListReceiveDataCoil(byte[] Arr);
}
