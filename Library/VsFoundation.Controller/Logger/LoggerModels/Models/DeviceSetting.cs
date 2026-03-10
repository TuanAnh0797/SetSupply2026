using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Logger.Enum;

namespace VsFoundation.Controller.Logger.LoggerModels.Models;

public class DeviceSetting
{
    public string NewAddress { get; set; } = "01";
    public eAnalogInputType InputType { get; set; } = eAnalogInputType.K;
    public ebaudrate Baudrate { get; set; } = ebaudrate.B9600;
}
