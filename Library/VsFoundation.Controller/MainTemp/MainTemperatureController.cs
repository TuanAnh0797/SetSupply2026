


using VsFoundation.Controller.Common.Models;
using VsFoundation.Controller.Common.Protocol.Interface;
using VsFoundation.Controller.MainTemp.Common;
using VsFoundation.Controller.MainTemp.Enum;
using VsFoundation.Controller.MainTemp.Interface;
using VsFoundation.Controller.MainTemp.MainTempModels.Enum;
using VsFoundation.Controller.MainTemp.MainTempModels.Interface;
using VsFoundation.Controller.MainTemp.MainTempModels.Models;
using VsFoundation.Controller.MainTemp.MainTempModels.SPSeries;

namespace VsFoundation.Controller.MainTemp
{
    public class MainTemperatureController
    {
        public IDeviceMainTemperatureController Device { get; }
        public IConnectable Connectable { get; set; }
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public ConnectionConfiguration ConnectionConfiguration { get; set; }
        private Task _monitorTask;
        public event Action<IResultMainTemperatureController> MonitorDataEvent;
        public event Action<string> ErrorEvent;
        public MonitorResult Monitor_Result { get; set; }
        public eMainTemperatureControllerModel ModuleType { get; }
        public MainTemperatureController(eMainTemperatureControllerModel type, IProtocol protocol, string deviceID, IConnectable connectable, ConnectionConfiguration connectionConfiguration)
        {
            ModuleType = type;
            ConnectionConfiguration = connectionConfiguration;
            Device = DeviceFactoryMainTempController.Create(type, protocol, deviceID);
            Connectable = connectable;
            Connectable.CancellationTokenSource = _cancellationTokenSource;
            Connectable.StatusPort += Connectable_StatusPort;
        }

        private void Connectable_StatusPort(string portname, bool status)
        {
            if (status)
            {
                _ = OpenConnection();
            }
            else
            {
                _ = StopMonitor();
            }
        }
        public async Task<bool> OpenConnection()
        {
            bool IsStarted = await Connectable.Connect();
            if (IsStarted)
            {
                await StopMonitor();
                await StartMonitor();
            }
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
        }
        public async Task StartMonitor()
        {
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
                    var command = Device.MonitorData();
                    var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
                    var MonitorData = Device.ParseMonitorData(response);
                    MonitorDataEvent?.Invoke(MonitorData);
                    Monitor_Result = MonitorData.MonitorResult;
                    await Task.Delay(100, _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    ErrorEvent?.Invoke("Monitor:" + ex.Message);
                }
            }
        }
        public async Task<bool> PatternSetting(IConfigurationMainTemperatureController param)
        {
            await StopMonitor();
            _cancellationTokenSource = new CancellationTokenSource();
            Connectable.CancellationTokenSource = _cancellationTokenSource;
            var commands = Device.PatternSetting(param);
            foreach (var command in commands)
            {
                try
                {
                    var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command.Value, ConnectionConfiguration.TimeOut);
                    var result = Device.ParsesPatternSetting(command.Key, response, param);
                }
                catch (Exception ex)
                {
                    if (ex.Message != "PatternDeleteNotExist")
                    {
                        ErrorEvent?.Invoke("PatternSetting:" + ex.Message);
                        return false;
                    }
                    return false;
                }
            }
            await OpenConnection();
            return true;
        }
        public async Task<eMainTempMode> GetMode()
        {
            try
            {
                var command = Device.GetMode();
                var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
                var rs = Device.ParsesGetMode(response);
                return rs.Mode;
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke("GetMode:" + ex.Message);
                return eMainTempMode.Error;
            }
        }
        public async Task<bool> SetMode(eMainTempMode mode)
        {
            try
            {
                IConfigurationMainTemperatureController param = ConfigurationFactoryMainTempController.Create(ModuleType);
                param.Mode = mode;
                var command = Device.SetMode(param);
                var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
                var rs = Device.ParsesSetMode(response);
                return rs.ReadWriteSingleRegisterOK;
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke("SetMode:" + ex.Message);
                return false;
            }
        }
        public async Task<bool> SetHold()
        {
            try
            {
                var command = Device.SetHold();
                var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
                var rs = Device.ParsesSetHold(response);
                return rs.ReadWriteSingleRegisterOK;
            }
            catch (Exception ex)
            {

                ErrorEvent?.Invoke("SetMode:" + ex.Message);
                return false;
            }

        }
        public async Task<bool> SetResum()
        {
            try
            {
                var command = Device.SetResume();
                var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
                var rs = Device.ParsesSetResum(response);
                return rs.ReadWriteSingleRegisterOK;
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke("SetResum:" + ex.Message);
                return false;
            }

        }
        public async Task<bool?> GetHold()
        {
            try
            {
                var command = Device.GetHold();
                var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
                var rs = Device.ParsesGetHold(response);
                return rs.IsHold;
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke("GetHold:" + ex.Message);
                return null;
            }
        }
        public async Task<bool> SelectPattern(short PatternNo)
        {
            try
            {
                IConfigurationMainTemperatureController param = ConfigurationFactoryMainTempController.Create(ModuleType);
                param.PatternID = PatternNo;
                var command = Device.SelectPattern(param);
                var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
                var rs = Device.ParsesSelectPattern(response);
                return rs.ReadWriteSingleRegisterOK;
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke("SelectPattern:" + ex.Message);
                return false;
            }

        }
        public async Task<bool> SetPVEvent(eAlarmNo eAlarmNo, eAlarmType eAlarmType, short PVHighLow)
        {
            try
            {
                if (Device is SPSeriesDevice SPDevice)
                {
                    var commands = SPDevice.SetPVEvent(eAlarmNo, eAlarmType, PVHighLow);
                    foreach (var command in commands)
                    {
                        var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command.Value, ConnectionConfiguration.TimeOut);
                        SPDevice.ParsesSetPVEvent(command.Key, response, eAlarmNo, eAlarmType, PVHighLow);
                    }
                    return true;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception)
            {

                return false;
            }

        }
        public async Task<bool> SetTimeSignal(short PatternID, short SegmentNo, eTimeSignal Status)
        {
            try
            {
                if (Device is SPSeriesDevice SPDevice)
                {
                    var command = SPDevice.SetTimeSignal(PatternID, SegmentNo, Status);
                    var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
                    SPDevice.ParsesSetTimeSignal(response);

                    return true;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }
        public async Task<bool> SetCoolingParam(eCoolingState state)
        {
            try
            {
                if (Device is SPSeriesDevice SPDevice)
                {
                    var command = SPDevice.SetCoolingParam(state);
                    var response = await Connectable.SendCommandAndReceiveResponseFullyAsync(command, ConnectionConfiguration.TimeOut);
                    SPDevice.ParsesSetCoolingParam(response);
                    return true;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }
    }
}
