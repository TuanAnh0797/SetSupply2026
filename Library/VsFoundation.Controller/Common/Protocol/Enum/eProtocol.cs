using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.Common.Protocol.Enum;

public enum ComPortState
{
    Attached,
    Detached,
    Unknown
}
public enum eStatusCoil : byte
{
    On = 0xFF,
    Off = 0x00
}
