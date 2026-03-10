namespace VsVirtualKeyboard.Model;

public class KeyData
{
    // English Key Data
    public string DefaultKey = string.Empty;
    public string ShiftKey = string.Empty;

    // Korean Key Data
    public string KorKey = string.Empty;
    public string KorShiftKey = string.Empty;

    // Chinese Key Data
    public string ChnKey = string.Empty;
    public string ChnShiftKey = string.Empty;

    /// <summary>
    /// KeyData Contructor
    /// </summary>
    /// <param name="defaultKey"></param>
    /// <param name="shiftKey"></param>
    /// <param name="korKey"></param>
    /// <param name="korShiftKey"></param>
    /// <param name="chnKey"></param>
    /// <param name="chnShiftKey"></param>
    public KeyData(string defaultKey = "", string shiftKey = "", string korKey = "", string korShiftKey = "", string chnKey = "", string chnShiftKey = "")
    {
        DefaultKey = defaultKey;
        ShiftKey = shiftKey;
        KorKey = korKey;
        KorShiftKey = korShiftKey;
        ChnKey = chnKey;
        ChnShiftKey = chnShiftKey;
    }
}
