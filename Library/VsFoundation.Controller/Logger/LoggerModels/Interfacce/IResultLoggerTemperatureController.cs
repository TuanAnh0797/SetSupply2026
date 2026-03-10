using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Logger.LoggerModels.Models;
namespace VsFoundation.Controller.Logger.LoggerModels.Interfacce;
public interface IResultLoggerTemperatureController
{
    public MonitorData DataChanel { get; set; }
    public EnableChanel EnableChanel { get; set; }
    public DeviceSetting DeviceSetting { get; set; }
    public KeyValuePair<string,double> MonitorSingleChanelData { get; set; }
    public string StringResponse { get; set; }
    public bool SendOK { get; set; }



}
