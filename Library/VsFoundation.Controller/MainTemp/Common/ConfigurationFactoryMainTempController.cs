

using VsFoundation.Controller.MainTemp.Enum;
using VsFoundation.Controller.MainTemp.MainTempModels.Interface;
using VsFoundation.Controller.MainTemp.MainTempModels.SPSeries.Models;
using VsFoundation.Controller.MainTemp.MainTempModels.UP550.Models;
using VsFoundation.Controller.MainTemp.MainTempModels.UP55A.Models;

namespace VsFoundation.Controller.MainTemp.Common;

public static  class ConfigurationFactoryMainTempController
{
    public static IConfigurationMainTemperatureController Create(eMainTemperatureControllerModel type)
    {
        return type switch
        {
            eMainTemperatureControllerModel.UP55A => new UP55AConfig(),
            eMainTemperatureControllerModel.UP550 => new UP550Config(),
            eMainTemperatureControllerModel.SPSeries => new SPSeriesConfig(),
            _ => throw new ArgumentException("Invalid device type", nameof(type)),
        };
    }
}
