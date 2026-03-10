namespace VsFoundation.Controller.DIOBoard.DIOBoard.ICPDas.Models;

/// <summary>
/// 16 Outputs
/// </summary>
public class ICPDasDO7045
{
    public bool Channel1Out { get; set; } = false;
    public bool Channel2Out { get; set; } = false;
    public bool Channel3Out { get; set; } = false;
    public bool Channel4Out { get; set; } = false;
    public bool Channel5Out { get; set; } = false;
    public bool Channel6Out { get; set; } = false;
    public bool Channel7Out { get; set; } = false;
    public bool Channel8Out { get; set; } = false;
    public bool Channel9Out { get; set; } = false;
    public bool Channel10Out { get; set; } = false;
    public bool Channel11Out { get; set; } = false;
    public bool Channel12Out { get; set; } = false;
    public bool Channel13Out { get; set; } = false;
    public bool Channel14Out { get; set; } = false;
    public bool Channel15Out { get; set; } = false;
    public bool Channel16Out { get; set; } = false;

    public byte[] GetOutByteToSent()
    {
        byte lower = 0;
        byte upper = 0;

        if (Channel1Out) lower |= 1 << 0;
        if (Channel2Out) lower |= 1 << 1;
        if (Channel3Out) lower |= 1 << 2;
        if (Channel4Out) lower |= 1 << 3;
        if (Channel5Out) lower |= 1 << 4;
        if (Channel6Out) lower |= 1 << 5;
        if (Channel7Out) lower |= 1 << 6;
        if (Channel8Out) lower |= 1 << 7;

        if (Channel9Out) upper |= 1 << 0;
        if (Channel10Out) upper |= 1 << 1;
        if (Channel11Out) upper |= 1 << 2;
        if (Channel12Out) upper |= 1 << 3;
        if (Channel13Out) upper |= 1 << 4;
        if (Channel14Out) upper |= 1 << 5;
        if (Channel15Out) upper |= 1 << 6;
        if (Channel16Out) upper |= 1 << 7;

        return new byte[2] { lower, upper };
    }
    public void SetByte(byte upper, byte lower)
    {
        Channel1Out = (lower & (1 << 0)) != 0;
        Channel2Out = (lower & (1 << 1)) != 0;
        Channel3Out = (lower & (1 << 2)) != 0;
        Channel4Out = (lower & (1 << 3)) != 0;
        Channel5Out = (lower & (1 << 4)) != 0;
        Channel6Out = (lower & (1 << 5)) != 0;
        Channel7Out = (lower & (1 << 6)) != 0;
        Channel8Out = (lower & (1 << 7)) != 0;

        Channel9Out = (upper & (1 << 0)) != 0;
        Channel10Out = (upper & (1 << 1)) != 0;
        Channel11Out = (upper & (1 << 2)) != 0;
        Channel12Out = (upper & (1 << 3)) != 0;
        Channel13Out = (upper & (1 << 4)) != 0;
        Channel14Out = (upper & (1 << 5)) != 0;
        Channel15Out = (upper & (1 << 6)) != 0;
        Channel16Out = (upper & (1 << 7)) != 0;
    }
}
