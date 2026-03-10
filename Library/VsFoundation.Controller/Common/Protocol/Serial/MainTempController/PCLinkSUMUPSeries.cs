
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Common.Protocol.Enum;
using VsFoundation.Controller.Common.Protocol.Interface;

namespace VsFoundation.Controller.Common.Protocol.Serial.MainTempController;

public class PCLinkSUMUPSeries:IProtocol
{
    private string CalculateSUM(byte[] data)
    {
        int sum = 0;
        foreach (byte b in data)
        {
            sum += b;
        }
        byte checksum = (byte)(sum & 0xFF); // get lowest byte
        return checksum.ToString("X2");// to hex
    }

    public byte[] CreateRead(byte slaveId, ushort startAddress, ushort numRegisters)
    {
        //STX + Address + CPU No + Response time + Command + Data corresponding to command + CheckSum + ETX + CR
        string command = "WRD";
        string startAdd = "D" + (startAddress + 1).ToString("D4");
        List<byte> body = new List<byte>();
        body.AddRange(Encoding.ASCII.GetBytes(slaveId.ToString("D2")));//Address (2 character)
        body.AddRange(Encoding.ASCII.GetBytes("01"));// CPU No   (2 character)
        body.AddRange(Encoding.ASCII.GetBytes("0"));//Response time (1 character)
        body.AddRange(Encoding.ASCII.GetBytes(command));
        body.AddRange(Encoding.ASCII.GetBytes(startAdd));
        body.AddRange(Encoding.ASCII.GetBytes(","));
        body.AddRange(Encoding.ASCII.GetBytes(numRegisters.ToString("D2")));
        return CreateFrame(body);

    }
    public byte[] CreateWrite(byte slaveId, ushort startAddress, short value)
    {
        //STX + Address + CPU No + Response time + Command + Data corresponding to command + CheckSum + ETX + CR
        string command = "WWR";
        string startAdd = "D" + (startAddress + 1).ToString("D4");
        List<byte> body = new List<byte>();
        body.AddRange(Encoding.ASCII.GetBytes(slaveId.ToString("D2")));//Address (2 character)
        body.AddRange(Encoding.ASCII.GetBytes("01"));// CPU No   (2 character)
        body.AddRange(Encoding.ASCII.GetBytes("0"));//Response time (1 character)
        body.AddRange(Encoding.ASCII.GetBytes(command));
        body.AddRange(Encoding.ASCII.GetBytes(startAdd));
        body.AddRange(Encoding.ASCII.GetBytes(","));
        body.AddRange(Encoding.ASCII.GetBytes("01"));
        body.AddRange(Encoding.ASCII.GetBytes(","));
        body.AddRange(Encoding.ASCII.GetBytes(value.ToString("X4")));
        return CreateFrame(body);
    }
    public byte[] CreateWriteMultiple(byte slaveId, ushort startAddress, short[] values)
    {
        //STX + Address + CPU No + Response time + Command + Data corresponding to command + CheckSum + ETX + CR
        string command = "WWR";
        string startAdd = "D" + (startAddress + 1).ToString("D4");
        List<byte> body = new List<byte>();
        //body.Add(0x02); // STX=02
        body.AddRange(Encoding.ASCII.GetBytes(slaveId.ToString("D2")));//Address (2 character)
        body.AddRange(Encoding.ASCII.GetBytes("01"));// CPU No   (2 character)
        body.AddRange(Encoding.ASCII.GetBytes("0"));//Response time (1 character)
        body.AddRange(Encoding.ASCII.GetBytes(command));
        body.AddRange(Encoding.ASCII.GetBytes(startAdd));
        body.AddRange(Encoding.ASCII.GetBytes(","));
        body.AddRange(Encoding.ASCII.GetBytes(values.Count().ToString("D2")));
        body.AddRange(Encoding.ASCII.GetBytes(","));
        foreach (var tmp in values)
        {
            body.AddRange(Encoding.ASCII.GetBytes(tmp.ToString("X4")));
        }
        return CreateFrame(body);
    }
    public List<short> GetListReceiveData(byte[] Arr)
    {
        //sent: {02}01010WRDD8003,0680{03}{0D}
        //get: {02}0101OK000000000000000000000000DC{03}{0D}
        //get error: {02}0101ER4400WRD0E{03}{0D}
        List<short> list = new List<short>();
        try
        {
            string status = Encoding.ASCII.GetString(Arr.Skip(5).Take(2).ToArray());
            var data = Arr.Skip(7).Take(Arr.Count() - 11).ToArray();
            for (int i = 0; i < data.Length; i = i + 4)
            {
                list.Add(Convert.ToInt16(Encoding.ASCII.GetString(data.Skip(i).Take(4).ToArray()), 16));
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
            if (Arr.Length < 10) { err = $"Valid Fail: Not enough bytes {Arr.Length}"; return ret; }
            if (Arr[0] != 0x02) { err = $"Valid Fail: Start code not correct - 0x02"; return ret; }
            if (Arr[Arr.Length - 1] != 0x0D) { err = $"Valid Fail: CR code not correct - 0x0D"; return ret; }
            if (Arr[Arr.Length - 2] != 0x03) { err = $"Valid Fail: End code not correct - 0x0D"; return ret; }
            string status = Encoding.ASCII.GetString(Arr.Skip(5).Take(2).ToArray());
            if (status == "OK") { }
            var body = Arr.Skip(1).Take(Arr.Count() - 5).ToArray();
            var crcBody = Arr.Skip(Arr.Count() - 4).Take(2).ToArray();
            string CRC = CalculateSUM(body.ToArray());
            ret = CRC == Encoding.ASCII.GetString(crcBody);
            if (!ret) { err = "Valid Fail: Checksum not correct"; return ret; }
            
        }
        catch (Exception ex) { err = ex.Message; ret = false; }
        return ret;
    }

    //11byte: STX(1),Address(2),CPUStation(2),Result(2),checksum(2),etx(1),cr(1)
    public bool ParsesWriteSingleRegister(byte[] dataParse)
    {
        if (dataParse.Length != 11) throw new Exception("Send Fail");
        //string strvalue = Encoding.ASCII.GetString(dataParse, 5, 2);
        return true;
    }
    public short ParsesReadSingleRegister(byte[] dataParse)
    {
        if (dataParse.Length != 15) throw new Exception("Incorrect array returned");
        string strvalue = Encoding.ASCII.GetString(dataParse, 7, 4);
        return short.Parse(strvalue);
    }
    #region common
    /// <summary>
    /// Creates a complete frame with STX, body, checksum, ETX, and CR
    /// </summary>
    /// <param name="body">The body bytes of the message</param>
    /// <returns>Complete byte array ready for transmission</returns>
    private byte[] CreateFrame(List<byte> body)
    {
        List<byte> final = new List<byte>();
        final.Add(0x02); // STX=02
        final.AddRange(body);
        final.AddRange(Encoding.ASCII.GetBytes(CalculateSUM(body.ToArray())));
        final.Add(0x03); // ETX=0x03
        final.Add(0x0D); // CR
        return final.ToArray();
    }

    public byte[] CreateReadCoil(byte slaveId, ushort startAddress, ushort numRegisters)
    {
        throw new NotImplementedException();
    }

    public byte[] CreateWriteSingleCoil(byte slaveId, ushort startAddress, eStatusCoil value)
    {
        throw new NotImplementedException();
    }

    public List<short> GetListReceiveDataCoil(byte[] Arr)
    {
        throw new NotImplementedException();
    }


    #endregion
}
