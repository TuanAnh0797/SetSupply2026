using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Controller.MainTemp.MainTempModels.Enum;

public enum eLinkCode
{
    RST = 0,
    HOLD = 1,
    PTN1 = 2,
    PTN2 = 3,
}
public enum eAlarmNo
{
    Alarm1 = 401,
    Alarm2 = 402,
    Alarm3 = 403,
    Alarm4 = 404,
}
public enum eAlarmType
{
    UpperOfPV = 1,
    LowerOfPV = 2,
   
}
public enum eTimeSignal
{
    Off = 0,
    On = 1,
}
public enum eCoolingState
{
    Off = 0,
    On = 15,
}


