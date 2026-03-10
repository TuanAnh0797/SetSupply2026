using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.MainTemp.MainTempModels.Enum;
using VsFoundation.Controller.MainTemp.MainTempModels.Interface;
using VsFoundation.Controller.MainTemp.MainTempModels.Models;

namespace VsFoundation.Controller.MainTemp.MainTempModels.UP550.Models;

public class UP550Config : IConfigurationMainTemperatureController
{
    public eMainTempMode Mode { get; set; }
    public short PatternID { get; set; }
    public Pattern Pattern { get; set; } = new Pattern();
    public List<Segment> Segments { get; set; } = new List<Segment>();
    public short SetpointLocalMode { get; set; }
    public bool UseLocalMode { get; set; } = false;
}
