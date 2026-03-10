
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Common.Protocol.Interface;
using VsFoundation.Controller.Common.Protocol.Serial.LoggerTempController;
using VsFoundation.Controller.Logger.Enum;

namespace VsFoundation.Controller.Logger.Common;

 public static class ProtocolFactoryLoggerTempController
{
    public static IProtocol Create(eLoggerTemperatureControllerModel type)
    {
        switch (type)
        {
            case eLoggerTemperatureControllerModel.I7018:
                    return new DconI7018();
            default:
                throw new ArgumentException("Invalid device type", nameof(type));
        }

    }
}
