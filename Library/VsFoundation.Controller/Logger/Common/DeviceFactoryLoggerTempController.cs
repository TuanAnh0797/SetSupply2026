using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Logger.Enum;
using VsFoundation.Controller.Logger.Interface;
using VsFoundation.Controller.Logger.LoggerModels.I7018;

namespace VsFoundation.Controller.Logger.Common;
public class DeviceFactoryLoggerTempController
{
    public static IDeviceLoggerTemperatureController Create(eLoggerTemperatureControllerModel type, int deviceId)
    {
        return type switch
        {
            eLoggerTemperatureControllerModel.I7018 => new I7018Device(deviceId),
            _ => throw new ArgumentException("Invalid device type", nameof(type)),
        };
    }
}
