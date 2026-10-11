using APP.Database;
using APP.Helper;
using APP.ViewModels.UserControlViewModels.Setting.Sub;
using SetSupply.Service;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace APP.Service;

/// <summary>
/// Vòng giao tiếp PLC (chạy nền):
/// - Giữ 1 kết nối TCP, mất kết nối thì 5 giây sau kết nối lại.
/// - Mỗi 300ms đọc bit trigger; mỗi 1 giây ghi bit Alive.
/// - Trigger = ON: đọc mã (12 ký tự model + 7 ký tự serial) => LƯU DATABASE => reset trigger (xác nhận với PLC).
///   Lưu lỗi thì KHÔNG reset trigger để lần quét sau thử lại (không mất sản phẩm).
/// - DataReceived chỉ phát sau khi đã lưu thành công (UI chỉ cần tải lại).
/// </summary>
public class PLCService
{
    public const int CodeLength = 19;
    public const int ModelLength = 12;
    public const int SerialLength = 7;

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan AliveInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SaveRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
    private const int CommandTimeoutMs = 3000;

    private readonly AppDbContext _db;
    private readonly ProductionDataService _productionService;
    private readonly PLC Plc = new PLC();

    private sealed record PlcConfig(IPAddress Ip, int Port, int AddressData, int TotalRegisterData, int AddressTrigger, int AddressAlive);

    private volatile PlcConfig? _config;
    private volatile string? _configError = "Chưa cài đặt PLC";

    // Task control
    private Task? _readTask;
    private CancellationTokenSource? _cts;
    private readonly object _runSync = new();

    // Events
    /// <summary>Phát (trên thread nền) sau khi 1 sản phẩm mới đã được lưu vào database. Tham số: mã đọc được.</summary>
    public event Action<string>? DataReceived;
    /// <summary>Phát (trên thread nền) khi trạng thái kết nối thay đổi.</summary>
    public event Action<bool>? ConnectionChanged;

    private bool _isConnected;
    public bool IsConnected => _isConnected;

    public PLCService(AppDbContext db, UCPLCSettingViewModel settingViewModel, ProductionDataService productionService)
    {
        _db = db;
        _productionService = productionService;
        settingViewModel.PLCConfigChanged += OnPLCConfigChanged;
        LoadConfig();
    }

    private void OnPLCConfigChanged()
    {
        LoadConfig();

        // Khởi động lại vòng giao tiếp với cấu hình mới
        bool wasRunning;
        lock (_runSync) wasRunning = _readTask != null;
        if (wasRunning)
        {
            Stop();
            Start();
        }
    }

    private void LoadConfig()
    {
        try
        {
            var config = _db.PLCSetting.FirstOrDefault();
            if (config == null)
            {
                SetConfig(null, "Chưa cài đặt PLC (Cài đặt > PLC Setting)");
                return;
            }

            if (!IPAddress.TryParse(config.IPAddress?.Trim(), out var ip))
            {
                SetConfig(null, $"Địa chỉ IP PLC không hợp lệ: '{config.IPAddress}'");
                return;
            }

            if (config.Port < 1 || config.Port > 65535)
            {
                SetConfig(null, $"Port PLC không hợp lệ: {config.Port}");
                return;
            }

            int minWords = (CodeLength + 1) / 2;
            if (config.TotalRegisterData < minWords)
            {
                SetConfig(null, $"TotalRegisterData = {config.TotalRegisterData} word, cần ít nhất {minWords} word để đọc đủ {CodeLength} ký tự");
                return;
            }

            SetConfig(new PlcConfig(ip, config.Port, config.AddressData, config.TotalRegisterData,
                                    config.AddressTrigger, config.AddressAlive), null);
        }
        catch (Exception ex)
        {
            SetConfig(null, $"Lỗi đọc cài đặt PLC: {ex.Message}");
        }
    }

    private void SetConfig(PlcConfig? config, string? error)
    {
        _config = config;
        _configError = error;
        if (error != null) PlcLogger.Warn(error);
    }

    public void Start()
    {
        lock (_runSync)
        {
            if (_readTask != null) return;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _readTask = Task.Run(() => RunLoop(token));
        }
        PlcLogger.Info("PLC service started");
    }

    public void Stop()
    {
        Task? task;
        CancellationTokenSource? cts;
        lock (_runSync)
        {
            task = _readTask;
            cts = _cts;
            _readTask = null;
            _cts = null;
        }
        if (task == null) return;

        cts?.Cancel();
        try { task.Wait(TimeSpan.FromSeconds(3)); } catch { }
        if (task.IsCompleted) cts?.Dispose();

        UpdateConnectionStatus(false);
        PlcLogger.Info("PLC service stopped");
    }

    private async Task RunLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var config = _config;
            if (config == null)
            {
                UpdateConnectionStatus(false);
                PlcLogger.ErrorThrottled(_configError ?? "Chưa cài đặt PLC");
                if (!await DelaySafe(ReconnectDelay, token)) break;
                continue;
            }

            try
            {
                using var client = new TcpClient { NoDelay = true };
                using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    connectCts.CancelAfter(ConnectTimeout);
                    try
                    {
                        await client.ConnectAsync(config.Ip, config.Port, connectCts.Token);
                    }
                    catch (OperationCanceledException) when (!token.IsCancellationRequested)
                    {
                        throw new TimeoutException($"Kết nối PLC {config.Ip}:{config.Port} quá {ConnectTimeout.TotalSeconds:0}s");
                    }
                }

                var stream = client.GetStream();
                var lastAlive = DateTime.MinValue;

                while (!token.IsCancellationRequested && ReferenceEquals(config, _config))
                {
                    if (DateTime.Now - lastAlive >= AliveInterval)
                    {
                        await Plc.WriteBit(stream, CommandTimeoutMs, PLC.PlcDeviceType.M, config.AddressAlive, true);
                        lastAlive = DateTime.Now;
                    }

                    // Có phản hồi đúng giao thức mới coi là đã kết nối
                    bool trigger = await Plc.ReadBit(stream, CommandTimeoutMs, PLC.PlcDeviceType.M, config.AddressTrigger);
                    if (!_isConnected)
                    {
                        UpdateConnectionStatus(true);
                        PlcLogger.ResetThrottle();
                    }

                    if (trigger)
                        await HandleTriggerAsync(stream, config, token);

                    if (!await DelaySafe(PollInterval, token)) break;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                UpdateConnectionStatus(false);
                PlcLogger.ErrorThrottled($"{ex.GetType().Name}: {ex.Message}");
            }

            if (!await DelaySafe(ReconnectDelay, token)) break;
        }

        UpdateConnectionStatus(false);
    }

    /// <summary>
    /// Đọc mã, lưu database, rồi mới reset trigger.
    /// </summary>
    private async Task HandleTriggerAsync(NetworkStream stream, PlcConfig config, CancellationToken token)
    {
        string data = ((string)await Plc.ReadData(stream, CommandTimeoutMs, PLC.PlcDeviceType.D,
                                                   config.AddressData, config.TotalRegisterData, PLC.DataType.ASCII)).TrimEnd();

        if (data.Length < CodeLength)
        {
            // Mã sai định dạng: ghi log rồi vẫn xác nhận để PLC không bị kẹt chờ
            PlcLogger.Warn($"Bỏ qua mã sai định dạng (cần >= {CodeLength} ký tự): '{data}'");
            await Plc.WriteBit(stream, CommandTimeoutMs, PLC.PlcDeviceType.M, config.AddressTrigger, false);
            return;
        }

        string model = data.Substring(0, ModelLength).Trim();
        string serial = data.Substring(ModelLength, SerialLength);

        bool inserted;
        try
        {
            inserted = await _productionService.TryInsertProductionAsync(data, model, serial);
        }
        catch (Exception ex)
        {
            // Lỗi database: không reset trigger => lần quét sau thử lưu lại
            PlcLogger.ErrorThrottled($"Lưu database lỗi, sẽ thử lại ({data}): {ex.GetBaseException().Message}");
            await DelaySafe(SaveRetryDelay, token);
            return;
        }

        await Plc.WriteBit(stream, CommandTimeoutMs, PLC.PlcDeviceType.M, config.AddressTrigger, false);
        PlcLogger.Info(inserted ? $"Nhận: {data}" : $"Trùng (bỏ qua): {data}");

        if (inserted)
        {
            try
            {
                DataReceived?.Invoke(data);
            }
            catch (Exception ex)
            {
                PlcLogger.Error($"Lỗi xử lý DataReceived: {ex.Message}");
            }
        }
    }

    private void UpdateConnectionStatus(bool connected)
    {
        if (_isConnected == connected) return;
        _isConnected = connected;
        PlcLogger.Info(connected ? "PLC connected" : "PLC disconnected");
        try
        {
            ConnectionChanged?.Invoke(connected);
        }
        catch (Exception ex)
        {
            PlcLogger.Error($"Lỗi xử lý ConnectionChanged: {ex.Message}");
        }
    }

    private static async Task<bool> DelaySafe(TimeSpan delay, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
