using System.Text;

namespace VsFoundation.Controller.TempLimit.TempLimitController.Common.Communication;

public class PCLinkVXFrame : IProtocolFrame
{
    public byte[] CreateRead(byte slaveId, ushort startAddress, ushort numRegisters)
    {
        string command = "DRR";
        List<byte> body = new List<byte>();
        //body.Add(0x02); // STX=02
        body.AddRange(Encoding.ASCII.GetBytes(slaveId.ToString("D2")));//Address (2 character)
        body.AddRange(Encoding.ASCII.GetBytes(command)); //(3 character)
        body.AddRange(Encoding.ASCII.GetBytes(","));
        body.AddRange(Encoding.ASCII.GetBytes(numRegisters.ToString("D2")));
        for (int i = 0; i < numRegisters; i++)
        {
            body.AddRange(Encoding.ASCII.GetBytes(","));
            body.AddRange(Encoding.ASCII.GetBytes((startAddress + i).ToString("D4")));
        }
        //----------------------//
        List<byte> final = new List<byte>();
        final.Add(0x02); // STX=02
        final.AddRange(body);
        final.AddRange(Encoding.ASCII.GetBytes(CalculateSUM(body.ToArray())));
        final.Add(0x0D); // CR
        final.Add(0x0A); // LF
        return final.ToArray();
    }

    public byte[] CreateWrite(byte slaveId, ushort startAddress, short[] values)
    {
        string command = "DWR";
        List<byte> body = new List<byte>();
        //body.Add(0x02); // STX=02
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
        //----------------------//
        List<byte> final = new List<byte>();
        final.Add(0x02); // STX=02
        final.AddRange(body);
        final.AddRange(Encoding.ASCII.GetBytes(CalculateSUM(body.ToArray())));
        final.Add(0x0D); // CR
        final.Add(0x0A); // LF
        return final.ToArray();
    }

    public List<short> GetListReceiveData(byte[] Arr)
    {
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
            if (Arr.Length < 10) { err = $"Not enough bytes {Arr.Length}"; return false; }
            if (Arr[0] != 0x02) { err = $"Start code not correct - 0x02"; return false; }
            if (Arr[Arr.Length - 1] != 0x0A) { err = $"LF code not correct"; return false; }
            if (Arr[Arr.Length - 2] != 0x0D) { err = $"CR code not correct"; return false; }
            string status = Encoding.ASCII.GetString(Arr.Skip(7).Take(2).ToArray());
            var body = Arr.Skip(1).Take(Arr.Count() - 5).ToArray();
            var crcBody = Arr.Skip(Arr.Count() - 4).Take(2).ToArray();
            string CRC = CalculateSUM(body.ToArray());
            ret = CRC == Encoding.ASCII.GetString(crcBody);
            if (!ret) { err = "Checksum not correct"; return false; }
            ret = status == "OK";
        }
        catch (Exception ex) { err = ex.Message; ret = false; }
        return ret;
    }
    #region Helpers
    private string CalculateSUM(byte[] data)
    {
        int sum = 0;
        foreach (byte b in data)
        {
            sum += b;
        }
        byte checksum = (byte)(sum & 0xFF);
        return checksum.ToString("X2");
    }
    #endregion
}
