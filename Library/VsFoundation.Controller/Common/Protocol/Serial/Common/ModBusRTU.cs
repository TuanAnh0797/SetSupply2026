
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Common.Protocol.Enum;
using VsFoundation.Controller.Common.Protocol.Interface;

namespace VsFoundation.Controller.Common.Protocol.Serial.Common;
public class ModBusRTU : IProtocol
{
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
    public byte[] CreateRead(byte slaveId, ushort startAddress, ushort numRegisters)
    {
        byte[] frame = new byte[8];
        frame[0] = slaveId;         // Slave ID
        frame[1] = 0x03;            // Function: Read Holding Registers
        frame[2] = (byte)(startAddress >> 8);  // Start address high byte
        frame[3] = (byte)(startAddress & 0xFF); // Start address low byte
        frame[4] = (byte)(numRegisters >> 8);  // Quantity high byte
        frame[5] = (byte)(numRegisters & 0xFF); // Quantity low byte
        return AppendCRC(frame, 6);
    }
    public byte[] CreateWrite(byte slaveId, ushort registerAddress, short value)
    {

        byte[] frame = new byte[8];
        frame[0] = slaveId;
        frame[1] = 0x06;  // Write Single Register
        frame[2] = (byte)(registerAddress >> 8);
        frame[3] = (byte)(registerAddress & 0xFF);
        frame[4] = (byte)(value >> 8);
        frame[5] = (byte)(value & 0xFF);
        return AppendCRC(frame, 6);
    }
    public List<short> GetListReceiveData(byte[] Arr)
    {
        //read = [Slave Address] [Function Code = 0x03] [Byte Count] [Data] [CRC]
        List<short> list = new List<short>();
        try
        {
            int dataLength = Arr[2];
            byte[] dataArr = Arr.Skip(3).ToArray();
            for (int i = 0; i < dataLength; i += 2)
            {
                var pair = dataArr.Skip(i).Take(2).ToArray();
                list.Add((short)(pair[0] << 8 | pair[1]));
            }
        }
        catch { list = new List<short>(); }
        return list;

    }
    public byte[] CreateWriteMultiple(byte slaveId, ushort startAddress, short[] values)
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
        return AppendCRC(frame, 6);
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
        return AppendCRC(frame, 6);
    }


    public bool ValidByteReceive(byte[] Arr, out string err)
    {
        //write => return it self
        //read = [Slave Address] [Function Code = 0x03] [Byte Count] [Data] [CRC]
        err = string.Empty;
        bool ret = false;
        try
        {
            if (Arr.Length < 5) { err = "Valid Fail: Not enough bytes"; return ret; }
            var crc = CalculateCRC(Arr.Take(Arr.Length - 2).ToArray(), Arr.Length - 2);
            ret = (byte)(crc & 0xFF) == Arr[Arr.Length - 2] && (byte)(crc >> 8) == Arr[Arr.Length - 1];
            if (!ret) { err = "Valid Fail: CRC Not Correct"; return ret; }
            ret = true;
        }
        catch (Exception ex)
        {
            err = ex.Message;
            ret = false;
        }
        return ret;
    }
    public bool ParsesWriteSingleRegister(byte[] dataParse)
    {
        if (dataParse.Length != 8) throw new Exception("Send Fail");
        return true;
    }
    public short ParsesReadSingleRegister(byte[] dataParse)
    {
        if (dataParse.Length != 7) throw new Exception("Incorrect array returned");
        short value = (short)(dataParse[3] << 8 | dataParse[4]);
        return value;
    }

    #region common
    /// <summary>
    /// Appends CRC to a frame and returns the frame with CRC
    /// </summary>
    /// <param name="frame">The frame to append CRC to</param>
    /// <param name="dataLength">Length of data to calculate CRC from</param>
    /// <returns>The frame with CRC appended</returns>
    private byte[] AppendCRC(byte[] frame, int dataLength)
    {
        ushort crc = CalculateCRC(frame, dataLength);
        frame[dataLength] = (byte)(crc & 0xFF);         // CRC Low
        frame[dataLength + 1] = (byte)(crc >> 8);       // CRC High
        return frame;
    }

    public List<short> GetListReceiveDataCoil(byte[] Arr)
    {
        //read = [Slave Address] [Function Code = 0x03] [Byte Count] [Data] [CRC]
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

    #endregion
}
