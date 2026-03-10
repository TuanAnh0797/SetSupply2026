
using System.Diagnostics;
using System.IO.Ports;
using VsFoundation.Controller.Common.Protocol.Enum;
using VsFoundation.Controller.Common.Protocol.Interface;
using VsFoundation.Controller.Common.Protocol.Models;


namespace VsFoundation.Controller.Common.Protocol.Serial.Common;

public class SerialConnect : ISerialConnect
{
    public bool IsConnected
    {
        get;
        set;
    }
    public event Action<string, byte[]> DataReceived;
    public event Action<string, bool> ConnectionChanged;
    public event Action<string, bool> StatusPort;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    private SerialPort _serialPort;
    private ComPortWatcherService? _comWatcher;
    private int timeoutOpen = 3000;
    private string _id;
    private string _comm;

    public int BaudRate { get; set; }
    public int DataBit { get; set; }
    public Parity Parity { get; set; }
    public StopBits StopBits { get; set; }
    public string Comm { get => _comm; set => _comm = value; }
    public string DeviceID { get => _id; set => _id = value; }

    public IProtocol ProtocolFrame;
    public CancellationTokenSource CancellationTokenSource { get; set; }


    public SerialConnect(SerialPortParam serialPortParam ,IProtocol protocol)
    {
        ProtocolFrame = protocol;
        _id = serialPortParam.Id;
        Comm = serialPortParam.Com; ;
        BaudRate = serialPortParam.BaudRate;
        Parity = serialPortParam.Parity; ;
        StopBits = serialPortParam.StopBits;
        DataBit = serialPortParam.DataBit;
        _serialPort = new SerialPort(Comm, BaudRate, Parity, DataBit, StopBits);
        InitComWatcher();
    }

    private void InitComWatcher()
    {
        _comWatcher = new ComPortWatcherService(Comm, async (portState) =>
        {
            try
            {
                if (portState == ComPortState.Detached)
                {
                    await Close();
                    StatusPort?.Invoke(Comm, false);
                }
                else if (portState == ComPortState.Attached)
                {
                    await Task.Delay(3000);

                    if (!await Connect())  // giả sử Connect trả về bool
                    {
                        StatusPort?.Invoke(Comm, false);
                        return;
                    }

                    Debug.WriteLine("Connected Serial");
                    StatusPort?.Invoke(Comm, true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Watcher error: {ex}");
            }
        });
    }
    public async Task<bool> Connect()
    {
        if (_serialPort == null)
        {
            NotifyChangeStatus(false);
            throw new Exception("Serial not initialized!");
        }
        if (_serialPort.IsOpen)
        {
            NotifyChangeStatus(true);
            return true;
        }
        _serialPort.BaudRate = BaudRate;
        _serialPort.Parity = Parity;
        _serialPort.DataBits = DataBit;
        _serialPort.StopBits = StopBits;
        _serialPort.ReadBufferSize = 10000;

        bool success = false;

        var openTask = Task.Run(() =>
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _serialPort.Open();
                success = true;
                IsConnected = true;
            }
            catch (Exception ex)
            {
                success = false;
            }
        });

        var completed = await Task.WhenAny(openTask, Task.Delay(timeoutOpen)) == openTask;

        if (!completed || openTask.IsFaulted || !success)
        {
            if (_serialPort.IsOpen)
                _serialPort.Close();

            NotifyChangeStatus(false);
            return false;
        }

        NotifyChangeStatus(true);
        return true;
    }

    public async Task Disconnect()
    {
        if (_serialPort == null) return;
        await Task.Delay(200);
        _serialPort.Close();
        NotifyChangeStatus(false);
        IsConnected = false;
        _comWatcher?.Dispose();
    }

    private async Task Close()
    {
        if (_serialPort == null) return;
        IsConnected = false;
        await Task.Delay(200);
        _serialPort.Close();
        NotifyChangeStatus(false);
    }

    public async Task<byte[]> SendCommandAndReceiveResponseFullyAsync(byte[] commandBytes, int timeout = 5000)
    {
        try
        {
            await _semaphore.WaitAsync();
            if (!_serialPort.IsOpen)
            {
                throw new InvalidOperationException("Serial port is not Open");
            }
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
            _serialPort.Write(commandBytes, 0, commandBytes.Length);
            await Task.Delay(50, CancellationTokenSource.Token);
            return await ReceiveResponseAsync(timeout);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    private async Task<byte[]> ReceiveResponseAsync(int timeout)
    {
        var buffer = new List<byte>();
        var startTime = DateTime.Now;

        while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
        {
            try
            {
                if (_serialPort.BytesToRead > 0)
                {
                    byte[] tempBuffer = new byte[_serialPort.BytesToRead];
                    int bytesRead = _serialPort.Read(tempBuffer, 0, tempBuffer.Length);
                    buffer.AddRange(tempBuffer);
                    string err = "";
                    if (ProtocolFrame.ValidByteReceive(buffer.ToArray(), out err))
                    {
                        break;
                    }
                    await Task.Delay(50, CancellationTokenSource.Token);
                    if (_serialPort.BytesToRead == 0)
                    {
                        break;
                    }
                }
                else
                {
                    await Task.Delay(10, CancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

        }
        return buffer.ToArray();
    }
    private void NotifyChangeStatus(bool status)
    {
        IsConnected = status;

        if (status)
            Debug.WriteLine("Notify connect");

        ConnectionChanged?.Invoke(DeviceID, status);
    }

    public void DiscardBuffer()
    {
        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();
    }
}
