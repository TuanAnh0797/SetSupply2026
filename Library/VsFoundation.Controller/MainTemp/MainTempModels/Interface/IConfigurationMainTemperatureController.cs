using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.MainTemp.MainTempModels.Enum;
using VsFoundation.Controller.MainTemp.MainTempModels.Models;

namespace VsFoundation.Controller.MainTemp.MainTempModels.Interface;

public interface IConfigurationMainTemperatureController
{
    public eMainTempMode Mode { get; set; }
    public short PatternID { get; set; }
    public Pattern Pattern { get; set; } 
    public List<Segment> Segments { get; set; }
    public short SetpointLocalMode { get; set; }
    public bool UseLocalMode { get; set; }
}
