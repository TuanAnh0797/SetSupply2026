using System.Text;
using VsFoundation.Controller.DIOBoard.DIOBoard.ICPDas.Models;

namespace VsFoundation.Controller.DIOBoard.DIOBoard.ICPDas.Services;

public static class ICPDasHelper
{
    public static bool ParseReadDI7051(string dataStr, out ICPDasDI7051 result)
    {
        result = new();
        //string dataStr = Encoding.ASCII.GetString(data);
        //if (!(dataStr.StartsWith(">") && dataStr.EndsWith("\r"))) { return false; }
        dataStr = dataStr.Replace(">", "").Replace("\r", "").Trim();

        result.SetByte(Convert.ToByte(dataStr.Substring(0, 2), 16), Convert.ToByte(dataStr.Substring(2, 2), 16));
        return true;
    }
    public static bool ParseReadDO7045(string dataStr, out ICPDasDO7045 result)
    {
        result = new();
        //string dataStr = Encoding.ASCII.GetString(data);
        //if (!(dataStr.StartsWith(">") && dataStr.EndsWith("\r"))) { return false; }
        dataStr = dataStr.Replace(">", "").Replace("\r", "").Trim();

        result.SetByte(Convert.ToByte(dataStr.Substring(0, 2), 16), Convert.ToByte(dataStr.Substring(2, 2), 16));
        return true;
    }

    public static bool ParseReadDIO7055(string dataStr, out ICPDasDIO7055 result)
    {
        result = new();
        //string dataStr = Encoding.ASCII.GetString(data);
        //if (!(dataStr.StartsWith(">") && dataStr.EndsWith("\r"))) { return false; }
        dataStr = dataStr.Replace(">", "").Replace("\r", "").Trim();

        result.SetByte(Convert.ToByte(dataStr.Substring(0, 2), 16), Convert.ToByte(dataStr.Substring(2, 2), 16));
        return true;

    }
}
