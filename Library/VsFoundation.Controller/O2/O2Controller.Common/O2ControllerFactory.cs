using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.O2.O2Controller.Toray.Services;
using VsFoundation.Controller.O2.O2Controller.ZR5.Services;
using VSLibrary.Communication;

namespace VsFoundation.Controller.O2.O2Controller.Common;

public static class O2ControllerFactory
{
    public static IO2Controller Create(eO2ControllerType type, ICommunication connection, CancellationToken _cancellationToken, int timeoutMs = 3000, int retryCount = 3)
    {
        return type switch
        {
            eO2ControllerType.ZR5 => new ZR5Control(connection, _cancellationToken, timeoutMs, retryCount),
            eO2ControllerType.TorayLC300 or
            eO2ControllerType.TorayLC450 or
            eO2ControllerType.TorayLC850KD or
            eO2ControllerType.TorayLC850KS or
            eO2ControllerType.TorayLC860 => new TorayControl(connection, type),
            _ => throw new NotSupportedException($"O2 controller '{type}' is not supported.")
        };
    }
}
