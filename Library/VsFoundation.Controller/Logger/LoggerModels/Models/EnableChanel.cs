using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.Logger.LoggerModels.Interfacce;

public class EnableChanel
{
    private bool _chanel1 = true;
    private bool _chanel2 = true;
    private bool _chanel3 = true;
    private bool _chanel4 = true;
    private bool _chanel5 = true;
    private bool _chanel6 = true;
    private bool _chanel7 = true;
    private bool _chanel8 = true;
    public bool Chanel1 { get => _chanel1; set => _chanel1 = value; }
    public bool Chanel2 { get => _chanel2; set => _chanel2 = value; }
    public bool Chanel3 { get => _chanel3; set => _chanel3 = value; }
    public bool Chanel4 { get => _chanel4; set => _chanel4 = value; }
    public bool Chanel5 { get => _chanel5; set => _chanel5 = value; }
    public bool Chanel6 { get => _chanel6; set => _chanel6 = value; }
    public bool Chanel7 { get => _chanel7; set => _chanel7 = value; }
    public bool Chanel8 { get => _chanel8; set => _chanel8 = value; }
    public string ToHexValue()
    {
        bool[] Channels = new bool[]
        {
            Chanel1, Chanel2, Chanel3, Chanel4, Chanel5, Chanel6, Chanel7, Chanel8
        };
        byte value = 0;
        for (int i = 0; i < 8; i++)
        {
            if (Channels[i])
                value |= (byte)(1 << i);
        }
        return value.ToString("X2");
    }
    public EnableChanel HexTo8Bits(string hexvalue)
    {
        byte value = Convert.ToByte(hexvalue, 16);
        Chanel1 = (value & (1 << 0)) != 0;
        Chanel2 = (value & (1 << 1)) != 0;
        Chanel3 = (value & (1 << 2)) != 0;
        Chanel4 = (value & (1 << 3)) != 0;
        Chanel5 = (value & (1 << 4)) != 0;
        Chanel6 = (value & (1 << 5)) != 0;
        Chanel7 = (value & (1 << 6)) != 0;
        Chanel8 = (value & (1 << 7)) != 0;
        return this;
    }
}
