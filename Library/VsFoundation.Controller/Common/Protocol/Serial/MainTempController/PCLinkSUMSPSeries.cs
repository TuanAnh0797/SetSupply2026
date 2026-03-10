
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Common.Protocol.Enum;
using VsFoundation.Controller.Common.Protocol.Interface;

namespace VsFoundation.Controller.Common.Protocol.Serial.MainTempController;

public class PCLinkSUMSPSeries: IProtocol
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
        string command = "RSD";
        List<byte> body = new List<byte>();
        body.AddRange(Encoding.ASCII.GetBytes(slaveId.ToString("D2")));//Address (2 character)
        body.AddRange(Encoding.ASCII.GetBytes(command)); //(3 character)
        body.AddRange(Encoding.ASCII.GetBytes(","));
        body.AddRange(Encoding.ASCII.GetBytes(numRegisters.ToString("D2")));
        for (int i = 0; i < numRegisters; i++)
        {
            body.AddRange(Encoding.ASCII.GetBytes(","));
            body.AddRange(Encoding.ASCII.GetBytes((startAddress + i).ToString("D4")));
        }
        return CreateFrame(body);
    }
    public byte[] CreateWrite(byte slaveId, ushort startAddress, short value)
    {
        string command = "WRD";
        List<byte> body = new List<byte>();
        body.AddRange(Encoding.ASCII.GetBytes(slaveId.ToString("D2")));//Address (2 character)
        body.AddRange(Encoding.ASCII.GetBytes(command)); //(3 character)
        body.AddRange(Encoding.ASCII.GetBytes(","));
        body.AddRange(Encoding.ASCII.GetBytes("01"));
        body.AddRange(Encoding.ASCII.GetBytes(","));
        body.AddRange(Encoding.ASCII.GetBytes(startAddress.ToString("D4")));
        body.AddRange(Encoding.ASCII.GetBytes(","));
        body.AddRange(Encoding.ASCII.GetBytes(value.ToString("D4")));
        //----------------------//
        return CreateFrame(body);
    }
    public byte[] CreateWriteMultiple(byte slaveId, ushort startAddress, short[] values)
    {
        string command = "WSD";
        List<byte> body = new List<byte>();
        body.AddRange(Encoding.ASCII.GetBytes(slaveId.ToString("D2")));//Address (2 character)
        body.AddRange(Encoding.ASCII.GetBytes(command)); //(3 character)
        body.AddRange(Encoding.ASCII.GetBytes(","));
        body.AddRange(Encoding.ASCII.GetBytes(values.Length.ToString("D2")));

        for (int i = 0; i < values.Length; i++)
        {
            body.AddRange(Encoding.ASCII.GetBytes(","));
            body.AddRange(Encoding.ASCII.GetBytes((startAddress + i).ToString("D4")));
            body.AddRange(Encoding.ASCII.GetBytes(","));
            body.AddRange(Encoding.ASCII.GetBytes(values[i].ToString("X4")));
        }
        return CreateFrame(body);
    }
    public List<short> GetListReceiveData(byte[] Arr)
    {
        //[STX] 01RSD,OK,01F4,012C19[CR][LF]
        List<short> list = new List<short>();
        try
        {
            string status = Encoding.ASCII.GetString(Arr.Skip(7).Take(2).ToArray());
            var data = Arr.Skip(10).Take(Arr.Count() - 14).ToArray();
            var dataStr = Encoding.ASCII.GetString(data);
            foreach (var tmp in dataStr.Split(','))
            {
                list.Add(Convert.ToInt16(tmp, 16));
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
            if (Arr.Length < 10) { err = $"Not enough bytes {Arr.Length}"; return ret; }
            if (Arr[0] != 0x02) { err = $"Start code not correct - 0x02"; return ret; }
            if (Arr[Arr.Length - 1] != 0x0A) { err = $"LF code not correct"; return ret; }
            if (Arr[Arr.Length - 2] != 0x0D) { err = $"CR code not correct"; return ret; }
            string status = Encoding.ASCII.GetString(Arr.Skip(7).Take(2).ToArray());
            var body = Arr.Skip(1).Take(Arr.Count() - 5).ToArray();
            var crcBody = Arr.Skip(Arr.Count() - 4).Take(2).ToArray();
            string CRC = CalculateSUM(body.ToArray());
            ret = CRC == Encoding.ASCII.GetString(crcBody);
            if (!ret) { err = "Checksum not correct"; return ret; }
            ret = status == "OK";
        }
        catch (Exception ex) { err = ex.Message; ret = false; }
        return ret;
    }
    public bool ParsesWriteSingleRegister(byte[] dataParse)
    {
        if (dataParse.Length != 13) throw new Exception("Send Fail");
        return true;
    }

    public short ParsesReadSingleRegister(byte[] dataParse)
    {
        if (dataParse.Length != 18) throw new Exception("Incorrect array returned");
        string strvalue = Encoding.ASCII.GetString(dataParse, 10, 4);
        return short.Parse(strvalue);
    }
    #region common
    /// <summary>
    /// Creates a complete frame with STX, body, checksum, CR, and LF
    /// </summary>
    /// <param name="body">The body bytes of the message</param>
    /// <returns>Complete byte array ready for transmission</returns>
    private byte[] CreateFrame(List<byte> body)
    {
        List<byte> final = new List<byte>();
        final.Add(0x02); // STX=02
        final.AddRange(body);
        final.AddRange(Encoding.ASCII.GetBytes(CalculateSUM(body.ToArray())));
        final.Add(0x0D); // CR
        final.Add(0x0A); // LF
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
