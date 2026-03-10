using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Logger.LoggerModels.Interfacce;
using VsFoundation.Controller.Logger.LoggerModels.Models;

namespace VsFoundation.Controller.Logger.LoggerModels.I7018.Models;

public class I7018Config : IConfigurationLoggerTemperatureController
{
    public EnableChanel EnableChanel { get ; set; } = new EnableChanel();
    public DeviceSetting DeviceSetting { get; set; } = new DeviceSetting();
    public string SingleChanelMonitor { get; set; } = "0";
}
