using System.Globalization;
using System.Text;

namespace VsFoundation.Controller.O2.O2Controller.ZR5.Services;

public static class ZR5Helper
{
    public static bool Parse(eZR5Command command, string dataStr, out float outValue)
    {
        outValue = 0;
        bool ret = false;
        switch (command)
        {
            case eZR5Command.Init:
                {
                    ret = dataStr.StartsWith("VV") && dataStr.EndsWith("\r");
                }
                break;
            case eZR5Command.Oxygen:
                {
                    ret = dataStr.StartsWith("M2") && dataStr.EndsWith("\r");
                    if (!ret) break;
                    ret = float.TryParse(dataStr.Replace("\r", "").Replace("M2", "").Trim(), CultureInfo.InvariantCulture, out outValue);
                }
                break;
            case eZR5Command.CellVoltage:
                {
                    ret = dataStr.StartsWith("A1") && dataStr.EndsWith("\r");
                    if (!ret) break;
                    ret = float.TryParse(dataStr.Replace("\r", "").Replace("A1", "").Trim(), CultureInfo.InvariantCulture, out outValue);
                }
                break;
            case eZR5Command.Temperature:
                {
                    ret = dataStr.StartsWith("A2") && dataStr.EndsWith("\r");
                    if (!ret) break;
                    ret = float.TryParse(dataStr.Replace("\r", "").Replace("A2", "").Trim(), CultureInfo.InvariantCulture, out outValue);
                }
                break;
        }
        return ret;
    }
}
public enum eZR5Command { Init, Temperature, CellVoltage, Oxygen }
