using VsFoundation.Controller.Common.Models;
using VsFoundation.Controller.Common.Protocol.Interface;
using VsFoundation.Controller.Logger.Common;
using VsFoundation.Controller.Logger.Enum;
using VsFoundation.Controller.Logger.Interface;
using VsFoundation.Controller.Logger.LoggerModels.Interfacce;
using VsFoundation.Controller.Logger.LoggerModels.Models;

namespace LoggerTemperatureLibrary;
public class LoggerTemperatureController
{
    public IDeviceLoggerTemperatureController Device { get; }
    public IConnectable Connectable { get; set; }
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    public ConnectionConfiguration ConnectionConfiguration { get; set; }
    private Task _monitorTask;
    public event Action<IResultLoggerTemperatureController> MonitorDataEvent;
    public event Action<string> ErrorEvent;
    public eLoggerTemperatureControllerModel ModuleType { get; }
    public LoggerTemperatureController(eLoggerTemperatureControllerModel type, int deviceID, IConnectable connectable, ConnectionConfiguration connectionConfiguration)
    {
        ModuleType = type;
        ConnectionConfiguration = connectionConfiguration;
        Device = DeviceFactoryLoggerTempController.Create(type, deviceID);
        Connectable = connectable;
        Connectable.CancellationTokenSource = _cancellationTokenSource;
        Connectable.StatusPort += Connectable_StatusPort;
    }
    private void Connectable_StatusPort(string portname, bool status)
    {
        if (status)
        {
            _ = OpenConnection();
            _ = StartMonitor();
        }
        else
        {
            _ = StopMonitor();
        }
    }
    public async Task<bool> OpenConnection()
    {
        bool IsStarted = await Connectable.Connect();
        //if (IsStarted)
        //{
        //    await StopMonitor();
        //    await StartMonitor();
        //}
        return IsStarted;
    }
    public async Task CloseConnection()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
        }

        if (_monitorTask != null)
        {
            if (!_monitorTask.IsCompleted)
            {
                await _monitorTask;
            }
        }
        await Connectable.Disconnect();
    }
    public async Task StopMonitor()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
        }
        if (_monitorTask != null)
        {
            if (!_monitorTask.IsCompleted)
            {
                await _monitorTask;
            }
        }
        _cancellationTokenSource = new CancellationTokenSource();
        Connectable.CancellationTokenSource = _cancellationTokenSource;
    }
    public async Task StartMonitor()
    {

        if (!Connectable.IsConnected)
        {
            ErrorEvent?.Invoke("Port not Open");
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        Connectable.CancellationTokenSource = _cancellationTokenSource;
        _monitorTask = Task.Run(async () =>
        {
            try
            {
                await Monitor();
            }
            catch (OperationCanceledException ex)
            {
                ErrorEvent?.Invoke("OpenConnection:" + ex.Message);
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke("OpenConnection:" + ex.Message);
            }
        }, _cancellationTokenSource.Token);


    }
    private async Task Monitor()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var command = Device.MonitorAllChanel();
                var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
                var MonitorData = Device.ParsesMonitorAllChanel(response);
                MonitorDataEvent?.Invoke(MonitorData);
                await Task.Delay(100, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke("Monitor:" + ex.Message);
            }
        }
    }
    public async Task<bool> SetConfigurationModule(DeviceSetting deviceSetting)
    {
        try
        {
            IConfigurationLoggerTemperatureController config = ConfigurationFactoryLoggerTempController.Create(ModuleType);
            config.DeviceSetting = deviceSetting;
            var command =  Device.SetConfigurationModule(config);
            var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
            var result = Device.ParsesSetConfigurationModule(response, config);
            return result.SendOK;
        }
        catch (Exception ex)
        {
            ErrorEvent?.Invoke("SetConfigurationModule:" + ex.Message);
            return false;
        }
    }
    public async Task<IResultLoggerTemperatureController> GetConfigurationModule()
    {
        try
        {
            var command = Device.GetConfigurationModule();
            var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
            var result = Device.ParsesGetConfigurationModule(response);
            return result;
        }
        catch (Exception ex)
        {
            ErrorEvent?.Invoke("GetConfigurationModule:" + ex.Message);
            return null;
        }
    }
    public async Task<bool> SetEnableDisableChanel(EnableChanel enableChanel)
    {
        try
        {
            IConfigurationLoggerTemperatureController config = ConfigurationFactoryLoggerTempController.Create(ModuleType);
            config.EnableChanel = enableChanel;
            var command = Device.SetEnableDisableChanel(config);
            var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
            var result = Device.ParsesSetEnableDisableChanel(response);
            return result.SendOK;
        }
        catch (Exception ex)
        {
            ErrorEvent?.Invoke("SetConfigurationModule:" + ex.Message);
            return false;
        }
    }
    public async Task<IResultLoggerTemperatureController> GetEnableDisableChanel()
    {
        try
        {
            var command = Device.GetEnableDisableChanel();
            var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
            var result = Device.ParsesGetEnableDisableChanel(response);
            return result;
        }
        catch (Exception ex)
        {
            ErrorEvent?.Invoke("GetConfigurationModule:" + ex.Message);
            return null;
        }
    }
}
