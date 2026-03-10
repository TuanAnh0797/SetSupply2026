using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.MainTemp.MainTempModels.Enum;
using VsFoundation.Controller.MainTemp.MainTempModels.Models;

namespace VsFoundation.Controller.MainTemp.MainTempModels.Interface;

public interface IResultMainTemperatureController
{
    public eMainTempMode Mode { get; set; }
    public MonitorResult MonitorResult { get; set; }
    public Pattern Pattern { get; set; }
    public List<Segment> Segment { get; set; }
    public errorCodeReadWritePatternSegment ErrorCodeReadWritePatternSegment { get; set; }
    public bool ParsesOK { get; set; } 
    public bool IsHold { get; set; }
    public bool ReadWriteSingleRegisterOK { get; set; }
}
