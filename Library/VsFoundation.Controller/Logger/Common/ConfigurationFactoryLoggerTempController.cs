using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Logger.Enum;
using VsFoundation.Controller.Logger.LoggerModels.I7018.Models;
using VsFoundation.Controller.Logger.LoggerModels.Interfacce;

namespace VsFoundation.Controller.Logger.Common;
internal class ConfigurationFactoryLoggerTempController
{
    public static IConfigurationLoggerTemperatureController Create(eLoggerTemperatureControllerModel type)
    {
        return type switch
        {
            eLoggerTemperatureControllerModel.I7018 => new I7018Config(),
            _ => throw new ArgumentException("Invalid device type", nameof(type)),
        };
    }
}
