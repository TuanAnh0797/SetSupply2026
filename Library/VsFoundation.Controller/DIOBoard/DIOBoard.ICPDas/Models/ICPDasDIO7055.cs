using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.DIOBoard.DIOBoard.ICPDas.Models;

/// <summary>
/// 8 Inputs + 8 Outputs
/// </summary>
public class ICPDasDIO7055
{
    public bool Channel1In { get; set; } = false;
    public bool Channel2In { get; set; } = false;
    public bool Channel3In { get; set; } = false;
    public bool Channel4In { get; set; } = false;
    public bool Channel5In { get; set; } = false;
    public bool Channel6In { get; set; } = false;
    public bool Channel7In { get; set; } = false;
    public bool Channel8In { get; set; } = false;
    public bool Channel1Out { get; set; } = false;
    public bool Channel2Out { get; set; } = false;
    public bool Channel3Out { get; set; } = false;
    public bool Channel4Out { get; set; } = false;
    public bool Channel5Out { get; set; } = false;
    public bool Channel6Out { get; set; } = false;
    public bool Channel7Out { get; set; } = false;
    public bool Channel8Out { get; set; } = false;

    public byte GetOutByteToSent()
    {
        byte upper = 0;
        if (Channel1Out) upper |= 1 << 0;
        if (Channel2Out) upper |= 1 << 1;
        if (Channel3Out) upper |= 1 << 2;
        if (Channel4Out) upper |= 1 << 3;
        if (Channel5Out) upper |= 1 << 4;
        if (Channel6Out) upper |= 1 << 5;
        if (Channel7Out) upper |= 1 << 6;
        if (Channel8Out) upper |= 1 << 7;

        return upper;
    }
    public void SetByte(byte output, byte input)
    {

        Channel1Out = (output & (1 << 0)) != 0;
        Channel2Out = (output & (1 << 1)) != 0;
        Channel3Out = (output & (1 << 2)) != 0;
        Channel4Out = (output & (1 << 3)) != 0;
        Channel5Out = (output & (1 << 4)) != 0;
        Channel6Out = (output & (1 << 5)) != 0;
        Channel7Out = (output & (1 << 6)) != 0;
        Channel8Out = (output & (1 << 7)) != 0;

        Channel1In = (input & (1 << 0)) != 0;
        Channel2In = (input & (1 << 1)) != 0;
        Channel3In = (input & (1 << 2)) != 0;
        Channel4In = (input & (1 << 3)) != 0;
        Channel5In = (input & (1 << 4)) != 0;
        Channel6In = (input & (1 << 5)) != 0;
        Channel7In = (input & (1 << 6)) != 0;
        Channel8In = (input & (1 << 7)) != 0;
    }
}
