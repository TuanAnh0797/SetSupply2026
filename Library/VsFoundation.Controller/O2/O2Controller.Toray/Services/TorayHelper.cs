using System.Text;
using VsFoundation.Controller.O2.O2Controller.Common;

namespace VsFoundation.Controller.O2.O2Controller.Toray.Services;

public static class TorayHelper
{
    public static bool Parse(eO2ControllerType type, byte[] data, out O2Result outValue)
    {
        outValue = new();
        bool ret = false;
        switch (type)
        {
            case eO2ControllerType.TorayLC300: { ret = TryParseLC300(data, out outValue); } break;
            case eO2ControllerType.TorayLC450: { ret = TryParseLC450(data, out outValue); } break;
            case eO2ControllerType.TorayLC850KD: { ret = TryParseLC850KD(data, out outValue); } break;
            case eO2ControllerType.TorayLC850KS: { ret = TryParseLC850KS(data, out outValue); } break;
            case eO2ControllerType.TorayLC860: { ret = TryParseLC860(data, out outValue); } break;
        }
        return ret;
    }

    private static bool TryParseFrame(byte[] data, out string payload)
    {
        payload = string.Empty;

        if (data == null || data.Length < 3) return false;

        if (data[0] != 0x02 || data[^2] != 0x03 || data[^1] != 0x0D) return false;

        int length = data.Length - 3;
        if (length <= 0) return false;

        byte[] payloadBytes = new byte[length];
        Array.Copy(data, 1, payloadBytes, 0, length);

        payload = Encoding.ASCII.GetString(payloadBytes);

        return true;
    }
    private static bool TryParseLC300(byte[] data, out O2Result outValue)
    {
        string outData = string.Empty;
        float outOxygen = 0;
        outValue = new();
        if (TryParseFrame(data, out outData))
        {
            if (outData.Length < 7) return false;
            var oxygenStr = outData.Substring(1, 5);
            var unit = outData.Substring(6, 1);
            if (!float.TryParse(oxygenStr.Replace("-", "").Trim(), out outOxygen)) return false;
            outValue.Oxygen = outOxygen;
            if (unit == "A" || unit == "P" || unit == "%")
            {
                outValue.Unit = unit == "A" ? eO2Unit.Atm : unit == "P" ? eO2Unit.PPM : eO2Unit.Percent;
                return true;
            }
            return false;
        }
        return false;
    }
    private static bool TryParseLC450(byte[] data, out O2Result outValue)
    {
        string outData = string.Empty;
        float outOxygen = 0;
        outValue = new();
        if (TryParseFrame(data, out outData))
        {
            if (outData.Length < 7) return false;
            var oxygenStr = outData.Substring(1, 5);
            var unit = outData.Substring(6, 1);
            if (!float.TryParse(oxygenStr.Replace("-", "").Trim(), out outOxygen)) return false;
            outValue.Oxygen = outOxygen;
            if (unit == "A" || unit == "P" || unit == "%")
            {
                outValue.Unit = unit == "A" ? eO2Unit.Atm : unit == "P" ? eO2Unit.PPM : eO2Unit.Percent;
                return true;
            }
            return false;
        }
        return false;
    }
    private static bool TryParseLC850KD(byte[] data, out O2Result outValue)
    {
        string outData = string.Empty;
        float outOxygen = 0;
        outValue = new();
        if (TryParseFrame(data, out outData))
        {
            if (outData.Length < 7) return false;
            var oxygenStr = outData.Substring(1, 5);
            var unit = outData.Substring(6, 1);
            if (!float.TryParse(oxygenStr.Replace("-", "").Trim(), out outOxygen)) return false;
            outValue.Oxygen = outOxygen;
            if (unit == "P" || unit == "%")
            {
                outValue.Unit = unit == "P" ? eO2Unit.PPM : eO2Unit.Percent;
                return true;
            }
            return false;
        }
        return false;
    }
    private static bool TryParseLC850KS(byte[] data, out O2Result outValue)
    {
        string outData = string.Empty;
        float outOxygen = 0;
        outValue = new();
        if (TryParseFrame(data, out outData))
        {
            if (outData.Length < 10) return false;
            var oxygenStr = outData.Substring(2, 5);
            var unit = outData.Substring(7, 3).Trim();
            if (!float.TryParse(oxygenStr.Replace("-", "").Trim(), out outOxygen)) return false;
            outValue.Oxygen = outOxygen;
            if (unit == "ppb" || unit == "ppm" || unit == "%")
            {
                outValue.Unit = unit == "ppb" ? eO2Unit.PPB : unit == "ppm" ? eO2Unit.PPM : eO2Unit.Percent;
                return true;
            }
            return false;
        }
        return false;
    }
    private static bool TryParseLC860(byte[] data, out O2Result outValue)
    {
        string outData = string.Empty;
        float outOxygen = 0;
        outValue = new();
        if (TryParseFrame(data, out outData))
        {
            if (outData.Length < 8) return false;
            var oxygenStr = outData.Substring(2, 5);
            var unit = outData.Substring(7, 1);
            if (!float.TryParse(oxygenStr.Replace("-", "").Trim(), out outOxygen)) return false;
            outValue.Oxygen = outOxygen;
            if (unit == "A" || unit == "B" || unit == "P" || unit == "%")
            {
                outValue.Unit = unit == "A" ? eO2Unit.Atm : unit == "P" ? eO2Unit.PPM : unit == "B" ? eO2Unit.PPB : eO2Unit.Percent;
                return true;
            }
            return false;
        }
        return false;
    }

}
