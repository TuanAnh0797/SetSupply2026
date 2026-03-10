namespace VsFoundation.Controller.DIOBoard.DIOBoard.ICPDas.Models;

/// <summary>
/// 16 Inputs
/// </summary>
public class ICPDasDI7051
{
    public bool Channel1In { get; set; } = false;
    public bool Channel2In { get; set; } = false;
    public bool Channel3In { get; set; } = false;
    public bool Channel4In { get; set; } = false;
    public bool Channel5In { get; set; } = false;
    public bool Channel6In { get; set; } = false;
    public bool Channel7In { get; set; } = false;
    public bool Channel8In { get; set; } = false;
    public bool Channel9In { get; set; } = false;
    public bool Channel10In { get; set; } = false;
    public bool Channel11In { get; set; } = false;
    public bool Channel12In { get; set; } = false;
    public bool Channel13In { get; set; } = false;
    public bool Channel14In { get; set; } = false;
    public bool Channel15In { get; set; } = false;
    public bool Channel16In { get; set; } = false;

    public void SetByte(byte upper, byte lower)
    {
        Channel1In = (lower & (1 << 0)) != 0;
        Channel2In = (lower & (1 << 1)) != 0;
        Channel3In = (lower & (1 << 2)) != 0;
        Channel4In = (lower & (1 << 3)) != 0;
        Channel5In = (lower & (1 << 4)) != 0;
        Channel6In = (lower & (1 << 5)) != 0;
        Channel7In = (lower & (1 << 6)) != 0;
        Channel8In = (lower & (1 << 7)) != 0;

        Channel9In = (upper & (1 << 0)) != 0;
        Channel10In = (upper & (1 << 1)) != 0;
        Channel11In = (upper & (1 << 2)) != 0;
        Channel12In = (upper & (1 << 3)) != 0;
        Channel13In = (upper & (1 << 4)) != 0;
        Channel14In = (upper & (1 << 5)) != 0;
        Channel15In = (upper & (1 << 6)) != 0;
        Channel16In = (upper & (1 << 7)) != 0;
    }
}