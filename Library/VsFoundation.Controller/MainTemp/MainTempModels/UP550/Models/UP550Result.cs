using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.MainTemp.MainTempModels.Enum;
using VsFoundation.Controller.MainTemp.MainTempModels.Interface;
using VsFoundation.Controller.MainTemp.MainTempModels.Models;

namespace VsFoundation.Controller.MainTemp.MainTempModels.UP550.Models;

public class UP550Result : IResultMainTemperatureController
{
    public eMainTempMode Mode { get; set; }
    public MonitorResult MonitorResult { get; set; } = new MonitorResult();
    public Pattern Pattern { get; set; } = new Pattern();
    public List<Segment> Segment { get; set; } = new List<Segment>();
    public errorCodeReadWritePatternSegment ErrorCodeReadWritePatternSegment { get; set; } = errorCodeReadWritePatternSegment.OK;
    public bool ParsesOK { get; set; } = false;
    public bool IsHold { get; set; } = false;
    public bool ReadWriteSingleRegisterOK { get; set; } = false;
   
}

