
using System.IO.Ports;
using System.Management;
using VsFoundation.Controller.Common.Protocol.Enum;

namespace VsFoundation.Controller.Common.Protocol.Serial.Common;
public class ComPortWatcherService : IDisposable
{
    private readonly string _targetPort;
    private readonly Action<ComPortState> _onPortChanged;
    private ManagementEventWatcher _watcher;

    private string[] _lastKnownPorts;

    public ComPortWatcherService(string portName, Action<ComPortState> onChanged)
    {
        _targetPort = portName;
        _onPortChanged = onChanged;

        _lastKnownPorts = SerialPort.GetPortNames();

        var query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3");
        _watcher = new ManagementEventWatcher(query);
        _watcher.EventArrived += OnDeviceChanged;
        _watcher.Start();
    }

    private void OnDeviceChanged(object sender, EventArrivedEventArgs e)
    {
        var currentPorts = SerialPort.GetPortNames();
        bool isNowPresent = currentPorts.Contains(_targetPort.ToUpper());
        bool wasPresent = _lastKnownPorts.Contains(_targetPort.ToUpper());

        if (!wasPresent && isNowPresent)
        {
            _onPortChanged?.Invoke(ComPortState.Attached);
        }
        else if (wasPresent && !isNowPresent)
        {
            _onPortChanged?.Invoke(ComPortState.Detached);
        }

        _lastKnownPorts = currentPorts;
    }

    public void Dispose()
    {
        _watcher?.Stop();
        _watcher?.Dispose();
    }
}
