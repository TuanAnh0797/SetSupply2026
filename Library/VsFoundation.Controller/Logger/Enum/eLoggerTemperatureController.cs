using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.Logger.Enum;

public enum eLoggerTemperatureControllerModel
{
   I7018
}
public enum eAnalogInputType : byte
{
    J = 0x0E,
    K = 0x0F,
    T = 0x10,
    E = 0x11,
    R = 0x12,
    S = 0x13,
    B = 0x14,
    N = 0x15,
    C = 0x16,
    //L = 0x17, //can not set
    //M = 0x18  //can not set
}
public enum ebaudrate : byte
{
    B1200 = 0x03,
    B2400 = 0x04,
    B4800 = 0x05,
    B9600 = 0x06,
    B19200 = 0x07,
    B38400 = 0x08,
    B57600 = 0x09,
    B115200 = 0x0A,
}

//

