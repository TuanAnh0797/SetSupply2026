using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Controller.Logger.LoggerModels.Interfacce;

namespace VsFoundation.Controller.Logger.Interface;

public interface IDeviceLoggerTemperatureController
{
    public string Address { get; set; }
    public byte[] SetConfigurationModule(IConfigurationLoggerTemperatureController param);
    public IResultLoggerTemperatureController ParsesSetConfigurationModule(byte[] dataParse, IConfigurationLoggerTemperatureController param);
    public byte[] GetConfigurationModule();
    public IResultLoggerTemperatureController ParsesGetConfigurationModule(byte[] dataParse);
    public byte[] MonitorAllChanel();
    public IResultLoggerTemperatureController ParsesMonitorAllChanel(byte[] dataParse);
    public byte[] SetEnableDisableChanel(IConfigurationLoggerTemperatureController param);
    public IResultLoggerTemperatureController ParsesSetEnableDisableChanel(byte[] dataParse);
    public byte[] GetEnableDisableChanel();
    public IResultLoggerTemperatureController ParsesGetEnableDisableChanel(byte[] dataParse);
    public byte[] MonitorSingleChanel(IConfigurationLoggerTemperatureController param);
    public IResultLoggerTemperatureController ParsesMonitorSingleChanel(byte[] dataParse, IConfigurationLoggerTemperatureController param);
    public byte[] SynchronizedSampling();
}
