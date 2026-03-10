using VSLibrary.Communication;

namespace VsFoundation.Controller.TempLimit.TempLimitController.Common.Communication;

public interface IConnectionManage
{
    IProtocolFrame Frame { get; set; }
    ICommunication Connection { get; set; }
    Task<List<short>> ReadData(byte slaveId, ushort startAddress, ushort numRegisters);
    Task<bool> WriteData(byte slaveId, ushort startAddress, short[] values);
    Task WriteDataNotResponse(byte slaveId, ushort startAddress, short[] values);
}
