using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Logger.LoggerModels.Models;

namespace VsFoundation.Controller.Logger.LoggerModels.Interfacce;
public interface IConfigurationLoggerTemperatureController
{
    public EnableChanel EnableChanel { get; set; }
    public DeviceSetting DeviceSetting { get; set; }
    public string SingleChanelMonitor { get; set; }
}
