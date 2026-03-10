using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Logger.LoggerModels.Interfacce;
using VsFoundation.Controller.Logger.LoggerModels.Models;

namespace VsFoundation.Controller.Logger.LoggerModels.I7018.Models;

public class I7018Result : IResultLoggerTemperatureController
{
    public MonitorData DataChanel { get; set; } = new MonitorData();
    public EnableChanel EnableChanel { get ; set ; } = new EnableChanel();
    public DeviceSetting DeviceSetting { get ; set; } = new DeviceSetting();
    public string StringResponse { get ; set; } = string.Empty;
    public KeyValuePair<string, double> MonitorSingleChanelData { get ; set ; }
    public bool SendOK { get; set; } = false;
}
