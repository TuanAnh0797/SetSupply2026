using APP.Database;
using APP.Helper;
using APP.Models.Database;
using APP.ViewModels.UserControlViewModels.Setting.Sub;
using NLog.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace APP.Service;
public class PLCService
{
    private readonly AppDbContext _db;
    private readonly UCPLCSettingViewModel _settingViewModel;
    private readonly PLC Plc = new PLC();

    // Config từ database
    private string _ipAddress;
    private int _port;
    private int _addressData;
    private int _totalRegisterData;
    private int _addressTrigger;
    private int _addressConfirmFinalReadData;
    private int _addressAlive;

    // Task control
    private Task _readTask;
    private CancellationTokenSource _cts;
    private bool _isRunning;

    // Events
    public event Action<string> DataReceived;
    public event Action<bool> ConnectionChanged;

    // State
    private string _lastData = "";
    private bool _isConnected;

    public bool IsConnected => _isConnected;


   


    public PLCService(AppDbContext db, UCPLCSettingViewModel settingViewModel)
    {
        _db = db;
        _settingViewModel = settingViewModel;
        _settingViewModel.PLCConfigChanged += OnPLCConfigChanged;
        LoadConfig();
    }

    private void OnPLCConfigChanged()
    {
        LoadConfig();

        // Restart nếu đang chạy
        if (_isRunning)
        {
            Stop();
            Start();
        }
    }

    private void LoadConfig()
    {
        var config = _db.PLCSetting.FirstOrDefault();
        if (config == null) return;

        _ipAddress = config.IPAddress;
        _port = config.Port;
        _addressData = config.AddressData;
        _totalRegisterData = config.TotalRegisterData;
        _addressTrigger = config.AddressTrigger;
        _addressConfirmFinalReadData = config.AddressConnfirmFinalReadData;
        _addressAlive = config.AddressAlive;
    }

    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _cts = new CancellationTokenSource();
        _readTask = Task.Run(() => RunLoop(_cts.Token));
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();

        try { _readTask?.Wait(2000); } catch { }

        _cts?.Dispose();
        _cts = null;

        UpdateConnectionStatus(false);
    }

    private async Task RunLoop(CancellationToken token)
    {
        while (_isRunning && !token.IsCancellationRequested)
        {
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    // Kết nối với timeout
                    var connectTask = tcpClient.ConnectAsync(IPAddress.Parse(_ipAddress), _port);
                    var timeout = Task.Delay(5000, token);

                    if (await Task.WhenAny(connectTask, timeout) != connectTask)
                        throw new TimeoutException("PLC connection timeout");

                    await connectTask;
                    UpdateConnectionStatus(true);

                    NetworkStream stream = tcpClient.GetStream();


                    await Plc.WriteBit(stream, 5000, PLC.PlcDeviceType.M, _addressAlive, true);


                    var trigger = await Plc.ReadBit(stream, 5000, PLC.PlcDeviceType.M, _addressTrigger); 

                    if (!trigger)
                    {
                        // Trigger chưa active, có thể ghi log hoặc xử lý khác nếu cần
                        continue;
                    }
                    else
                    {
                        // Đọc data từ PLC
                        string data = (string)await Plc.ReadData( stream, 5000,PLC.PlcDeviceType.D, _addressData,_totalRegisterData,PLC.DataType.ASCII);
                        if (!string.IsNullOrEmpty(data) && data.Trim() != _lastData)
                        {
                            DataReceived?.Invoke(data.Trim());
                            _lastData = data.Trim();
                           await Plc.WriteBit(stream, 5000, PLC.PlcDeviceType.M, _addressTrigger, false); 
                        }
                        else
                        {
                            await Plc.WriteBit(stream, 5000, PLC.PlcDeviceType.M, _addressTrigger, false);
                        }

                    }
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                UpdateConnectionStatus(false);
                LogError(ex.ToString());
            }

            try { await Task.Delay(5000, token); }
            catch (TaskCanceledException) { break; }
        }
    }

    private void UpdateConnectionStatus(bool connected)
    {
        
            _isConnected = connected;
            ConnectionChanged?.Invoke(connected);
        
    }

    private static void LogError(string message)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plc_error.log");
            using var writer = new StreamWriter(logPath, true, Encoding.UTF8);
            writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }
        catch { }
    }
}



