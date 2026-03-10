
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Common.Protocol.Interface;
using VsFoundation.Controller.MainTemp.MainTempModels.Interface;

namespace VsFoundation.Controller.MainTemp.Interface;

public interface IDeviceMainTemperatureController
{
    public IProtocol Protocol { get; set; }
    public byte SlaveID { get; set; }

    #region Monitor
    public byte[] MonitorData();
    public IResultMainTemperatureController ParseMonitorData(byte[] dataParse);
    #endregion

    #region Operation
    public byte[] GetMode();
    public IResultMainTemperatureController ParsesGetMode(byte[] dataParse);
    public byte[] SetHold();
    public IResultMainTemperatureController ParsesSetHold(byte[] dataParse);
    public byte[] SetResume();
    public IResultMainTemperatureController ParsesSetResum(byte[] dataParse);
    public byte[] GetHold();
    public IResultMainTemperatureController ParsesGetHold(byte[] dataParse);
    public byte[] SetMode(IConfigurationMainTemperatureController param);
    public IResultMainTemperatureController ParsesSetMode(byte[] dataParse);
    public byte[] SelectPattern(IConfigurationMainTemperatureController param);
    public IResultMainTemperatureController ParsesSelectPattern(byte[] dataParse);
    #endregion

    #region Pattern Setting
    public Dictionary<string, byte[]> PatternSetting(IConfigurationMainTemperatureController param);
    public IResultMainTemperatureController ParsesPatternSetting(string CommandName, byte[] dataParse, IConfigurationMainTemperatureController param);
    #endregion
}



