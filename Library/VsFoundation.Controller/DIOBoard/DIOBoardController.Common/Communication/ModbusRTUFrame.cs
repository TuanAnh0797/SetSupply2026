using VsFoundation.Controller.Common.Protocol.Enum;

namespace VsFoundation.Controller.DIOBoard.DIOBoardController.Common.Communication;

public class ModbusRTUFrame : IProtocolFrame
{
    public byte[] CreateRead(byte slaveId, ushort startAddress, ushort numRegisters)
    {
        byte[] frame = new byte[8];
        frame[0] = slaveId;
        frame[1] = 0x03;
        frame[2] = (byte)(startAddress >> 8);
        frame[3] = (byte)(startAddress & 0xFF);
        frame[4] = (byte)(numRegisters >> 8);
        frame[5] = (byte)(numRegisters & 0xFF);

        ushort crc = CalculateCRC(frame, 6);
        frame[6] = (byte)(crc & 0xFF);
        frame[7] = (byte)(crc >> 8);
        return frame;
    }
    public byte[] CreateReadCoil(byte slaveId, ushort startAddress, ushort numRegisters)
    {
        byte[] frame = new byte[8];
        frame[0] = slaveId;         // Slave ID
        frame[1] = 0x01;            // Function: Read Single Coil
        frame[2] = (byte)(startAddress >> 8);
        frame[3] = (byte)(startAddress & 0xFF);
        frame[4] = (byte)(numRegisters >> 8);
        frame[5] = (byte)(numRegisters & 0xFF);

        ushort crc = CalculateCRC(frame, 6);
        frame[6] = (byte)(crc & 0xFF);
        frame[7] = (byte)(crc >> 8);
        return frame;
    }
    public byte[] CreateWrite(byte slaveId, ushort startAddress, short[] values)
    {
        int byteCount = values.Length * 2;
        List<byte> request = new List<byte>
            {
                slaveId,
                0x10,
                (byte)(startAddress >> 8),
                (byte)(startAddress & 0xFF),
                (byte)(values.Length >> 8),
                (byte)(values.Length & 0xFF),
                (byte)byteCount
             };
        foreach (short value in values)
        {
            request.Add((byte)(value >> 8));
            request.Add((byte)(value & 0xFF));
        }
        ushort crc = CalculateCRC(request.ToArray(), request.Count);
        request.Add((byte)(crc & 0xFF));
        request.Add((byte)(crc >> 8));
        return request.ToArray();
    }
    public byte[] CreateWriteSingleCoil(byte slaveId, ushort startAddress, eStatusCoil value)
    {
        byte[] frame = new byte[8];
        frame[0] = slaveId;         // Slave ID
        frame[1] = 0x05;            // Function: Write Single Coil
        frame[2] = (byte)(startAddress >> 8);
        frame[3] = (byte)(startAddress & 0xFF);
        frame[4] = (byte)value;
        frame[5] = 0x00; // Coil value low byte (0xFF00 for ON, 0x0000 for OFF)

        ushort crc = CalculateCRC(frame, 6);
        frame[6] = (byte)(crc & 0xFF);
        frame[7] = (byte)(crc >> 8);
        return frame;
    }
    public List<short> GetListReceiveData(byte[] Arr)
    {
        List<short> list = new List<short>();
        try
        {
            int dataLength = (int)Arr[2];
            byte[] dataArr = Arr.Skip(3).ToArray();
            for (int i = 0; i < dataLength; i += 2)
            {
                var pair = dataArr.Skip(i).Take(2).ToArray();
                list.Add((short)((pair[0] << 8) | pair[1]));
            }
        }
        catch { list = new List<short>(); }
        return list;
    }

    public List<short> GetListReceiveDataCoil(byte[] Arr)
    {
        List<short> list = new List<short>();
        try
        {
            int dataLength = Arr[2];
            byte[] dataArr = Arr.Skip(3).ToArray();
            for (int i = 0; i < dataLength; i++)
            {
                list.Add(dataArr[i]);
            }
        }
        catch { list = new List<short>(); }
        return list;
    }

    public bool ValidByteReceive(byte[] Arr, out string err)
    {
        err = string.Empty;
        bool ret = false;
        try
        {
            if (Arr.Length < 5) { err = "Not enough bytes"; return false; }
            var crc = CalculateCRC(Arr.Take(Arr.Length - 2).ToArray(), Arr.Length - 2);
            ret = ((byte)(crc & 0xFF)) == Arr[Arr.Length - 2] && ((byte)(crc >> 8)) == Arr[Arr.Length - 1];
            if (!ret) { err = "CRC Not Correct"; return false; }
            ret = true;
        }
        catch (Exception ex)
        {
            err = ex.Message;
            ret = false;
        }
        return ret;
    }

    #region Helper
    private ushort CalculateCRC(byte[] data, int length)
    {
        ushort crc = 0xFFFF;
        for (int pos = 0; pos < length; pos++)
        {
            crc ^= data[pos];
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc >>= 1;
                    crc ^= 0xA001;
                }
                else
                {
                    crc >>= 1;
                }
            }
        }
        return crc;
    }
    #endregion
}