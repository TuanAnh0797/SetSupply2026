using APP.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SetSupply.Models.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SetSupply.Service;

/// <summary>
/// Dữ liệu kế hoạch đã đọc từ file, đã cộng theo nhóm model (chưa lưu).
/// </summary>
public sealed class PlanImportPreview
{
    public string FilePath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public DateTime FileLastWrite { get; init; }
    public string SheetName { get; init; } = string.Empty;
    public int ModelCount { get; init; }
    /// <summary>Ngày -> danh sách kế hoạch theo nhóm / ca. Ngày có tổng = 0 => danh sách rỗng (xóa kế hoạch ngày đó).</summary>
    public Dictionary<DateTime, List<ProductionPlan>> PlansByDate { get; } = new();
    public List<string> Warnings { get; } = new();
    /// <summary>Model NR- trong file chưa được gán nhóm (vào "Khác").</summary>
    public List<string> UnmappedModels { get; } = new();

    public DateTime? From => PlansByDate.Count > 0 ? PlansByDate.Keys.Min() : null;
    public DateTime? To => PlansByDate.Count > 0 ? PlansByDate.Keys.Max() : null;
    public int TotalQuantity => PlansByDate.Values.SelectMany(p => p).Sum(p => p.Quantity);
}

/// <summary>
/// Import file kế hoạch (định dạng "Daily revise") vào Daily Plan + job tự động import khi có file mới trong thư mục.
/// - Job: theo dõi thư mục (FileSystemWatcher) + quét lại mỗi phút (phòng khi watcher bỏ sót, VD thư mục mạng).
/// - File được coi là "mới" khi chưa có trong nhật ký với cùng đường dẫn + kích thước + giờ sửa.
/// - File vừa chép xong (sửa trong 15 giây gần nhất) hoặc đang bị khóa => đợi lần quét sau.
/// </summary>
public class PlanImportService
{
    public const string SourceAuto = "Tự động";
    public const string SourceManual = "Thủ công";

    private static readonly string[] Extensions = { ".xlsx", ".xlsm" };
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppSettingService _settings;
    private readonly ModelGroupService _modelGroupService;
    private readonly ProductionPlanService _planService;

    private readonly SemaphoreSlim _scanLock = new(1, 1);
    private readonly object _sync = new();
    private FileSystemWatcher? _watcher;
    private Timer? _timer;

    /// <summary>Phát (thread nền) sau khi import thành công 1 file.</summary>
    public event Action<PlanImportPreview>? PlanImported;
    /// <summary>Phát (thread nền) khi trạng thái job / nhật ký thay đổi.</summary>
    public event Action? StatusChanged;

    public string Status { get; private set; } = "Chưa chạy";

    public PlanImportService(IServiceScopeFactory scopeFactory, AppSettingService settings,
                             ModelGroupService modelGroupService, ProductionPlanService planService)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _modelGroupService = modelGroupService;
        _planService = planService;
    }

    // ===== JOB =====

    /// <summary>
    /// Bật / tắt job theo cài đặt hiện tại. Gọi khi khởi động và sau khi lưu cài đặt.
    /// </summary>
    public void Start()
    {
        lock (_sync)
        {
            StopInternal();

            var folder = _settings.PlanImportFolder;
            if (!_settings.PlanImportEnabled)
            {
                SetStatus("Tự động import: TẮT");
                return;
            }
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                SetStatus($"Tự động import: thư mục không tồn tại '{folder}'");
                // Vẫn quét định kỳ: thư mục mạng có thể kết nối lại sau
                _timer = new Timer(_ => _ = ScanAsync(), null, ScanInterval, ScanInterval);
                return;
            }

            try
            {
                _watcher = new FileSystemWatcher(folder)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };
                _watcher.Created += (_, _) => ScheduleScan();
                _watcher.Changed += (_, _) => ScheduleScan();
                _watcher.Renamed += (_, _) => ScheduleScan();
            }
            catch (Exception ex)
            {
                _watcher = null;
                System.Diagnostics.Debug.WriteLine($"PlanImport watcher error: {ex.Message}");
            }

            // Quét ngay sau 3 giây, sau đó mỗi phút
            _timer = new Timer(_ => _ = ScanAsync(), null, TimeSpan.FromSeconds(3), ScanInterval);
            SetStatus($"Tự động import: BẬT — đang theo dõi '{folder}'");
        }
    }

    public void Stop()
    {
        lock (_sync) StopInternal();
    }

    private void StopInternal()
    {
        _watcher?.Dispose();
        _watcher = null;
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>
    /// Có thay đổi trong thư mục: quét sau SettleTime (đợi chép xong file).
    /// </summary>
    private void ScheduleScan()
    {
        lock (_sync) _timer?.Change(SettleTime + TimeSpan.FromSeconds(2), ScanInterval);
    }

    /// <summary>
    /// Quét thư mục, import các file mới theo thứ tự thời gian (file mới nhất import sau cùng => được ưu tiên).
    /// </summary>
    public async Task ScanAsync()
    {
        if (!_settings.PlanImportEnabled) return;
        if (!await _scanLock.WaitAsync(0)) return; // đang quét
        try
        {
            var folder = _settings.PlanImportFolder;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                SetStatus($"Tự động import: thư mục không tồn tại '{folder}'");
                return;
            }

            var files = new DirectoryInfo(folder).GetFiles()
                .Where(f => Extensions.Contains(f.Extension, StringComparer.OrdinalIgnoreCase)
                            && !f.Name.StartsWith("~$")                      // file tạm của Excel
                            && !f.Attributes.HasFlag(FileAttributes.Hidden))
                .OrderBy(f => f.LastWriteTime)
                .ToList();

            int imported = 0, waiting = 0;
            foreach (var file in files)
            {
                file.Refresh();
                if (DateTime.Now - file.LastWriteTime < SettleTime) { waiting++; continue; } // có thể đang chép
                if (await IsProcessedAsync(file)) continue;

                try
                {
                    var preview = await ReadAsync(file.FullName);
                    await SaveAsync(preview, SourceAuto);
                    imported++;
                }
                catch (IOException)
                {
                    waiting++; // file đang bị khóa / đang chép => thử lại lần quét sau, không ghi lỗi
                }
                catch (Exception ex)
                {
                    await WriteLogAsync(file, SourceAuto, false, 0, $"Lỗi: {ex.GetBaseException().Message}");
                }
            }

            SetStatus($"Tự động import: BẬT — quét lúc {DateTime.Now:HH:mm:ss}" +
                      (imported > 0 ? $", đã import {imported} file" : ", không có file mới") +
                      (waiting > 0 ? $", {waiting} file đang đợi chép xong" : ""));
        }
        catch (Exception ex)
        {
            SetStatus($"Tự động import: lỗi khi quét thư mục — {ex.Message}");
        }
        finally
        {
            _scanLock.Release();
        }
    }

    private async Task<bool> IsProcessedAsync(FileInfo file)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lastWrite = file.LastWriteTime;
        var logs = await db.PlanImportLogs.AsNoTracking()
            .Where(l => l.FilePath == file.FullName && l.FileSize == file.Length)
            .Select(l => l.FileLastWrite)
            .ToListAsync();
        return logs.Any(t => Math.Abs((t - lastWrite).TotalSeconds) < 1);
    }

    // ===== IMPORT =====

    /// <summary>
    /// Đọc file kế hoạch và cộng theo nhóm model (chưa lưu). Ném IOException nếu file đang bị khóa.
    /// </summary>
    public async Task<PlanImportPreview> ReadAsync(string path)
    {
        var info = new FileInfo(path);

        // Đọc vào bộ nhớ, cho phép file đang mở trong Excel
        var memory = new MemoryStream();
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            await fs.CopyToAsync(memory);
        memory.Position = 0;

        var shiftMap = _settings.PlanShiftMap;
        var sheetName = _settings.PlanSheetName;
        var parsed = await Task.Run(() => PlanFileParser.Parse(memory, shiftMap, sheetName));

        var mappings = ModelGroupService.ToLookup(await _modelGroupService.GetMappingsAsync());
        var groupNames = ModelGroupService.GetGroupNames(mappings.Values);

        var preview = new PlanImportPreview
        {
            FilePath = info.FullName,
            FileSize = info.Length,
            FileLastWrite = info.LastWriteTime,
            SheetName = parsed.SheetName,
            ModelCount = parsed.Models.Count
        };
        preview.Warnings.AddRange(parsed.Warnings);
        preview.UnmappedModels.AddRange(parsed.Models
            .Where(m => ModelGroupService.ResolveGroup(m, mappings) == ModelGroupService.OtherGroupName)
            .OrderBy(m => m));

        foreach (var (date, modelPlans) in parsed.ByDate)
        {
            var byGroup = new Dictionary<(string Group, int Shift), int>();
            foreach (var ((model, shift), qty) in modelPlans)
            {
                var key = (ModelGroupService.ResolveGroup(model, mappings), shift);
                byGroup[key] = byGroup.TryGetValue(key, out var q) ? q + qty : qty;
            }

            // Tên nhóm theo đúng tên đã cài đặt; số âm (dữ liệu file lỗi) => 0
            preview.PlansByDate[date] = byGroup
                .Where(kv => kv.Value > 0)
                .Select(kv => new ProductionPlan
                {
                    PlanDate = date,
                    GroupName = groupNames.FirstOrDefault(n => string.Equals(n, kv.Key.Group, StringComparison.OrdinalIgnoreCase)) ?? kv.Key.Group,
                    ShiftNumber = kv.Key.Shift,
                    Quantity = kv.Value
                })
                .ToList();
        }

        return preview;
    }

    /// <summary>
    /// Ghi kế hoạch (ghi đè các ngày có trong file) + nhật ký.
    /// </summary>
    public async Task SaveAsync(PlanImportPreview preview, string source)
    {
        await _planService.SaveDaysAsync(preview.PlansByDate);

        string message = $"{preview.PlansByDate.Count} ngày ({preview.From:dd/MM/yyyy} - {preview.To:dd/MM/yyyy}), " +
                         $"{preview.ModelCount} model NR-, tổng {preview.TotalQuantity.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture)}";
        if (preview.UnmappedModels.Count > 0)
            message += $"; {preview.UnmappedModels.Count} model chưa có nhóm => \"{ModelGroupService.OtherGroupName}\"";
        if (preview.Warnings.Count > 0)
            message += $"; cảnh báo: {string.Join(" / ", preview.Warnings.Take(5))}" + (preview.Warnings.Count > 5 ? " ..." : "");

        await WriteLogAsync(new FileInfo(preview.FilePath), source, true, preview.PlansByDate.Count, message,
                            preview.FileSize, preview.FileLastWrite);
        try { PlanImported?.Invoke(preview); } catch { /* lỗi ở UI không làm hỏng import */ }
    }

    public async Task<List<PlanImportLog>> GetRecentLogsAsync(int count = 30)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.PlanImportLogs.AsNoTracking()
            .OrderByDescending(l => l.Id)
            .Take(count)
            .ToListAsync();
    }

    private async Task WriteLogAsync(FileInfo file, string source, bool success, int days, string message,
                                     long? size = null, DateTime? lastWrite = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PlanImportLogs.Add(new PlanImportLog
            {
                FilePath = file.FullName,
                FileName = file.Name,
                FileSize = size ?? (file.Exists ? file.Length : 0),
                FileLastWrite = lastWrite ?? (file.Exists ? file.LastWriteTime : DateTime.MinValue),
                ImportedAt = DateTime.Now,
                Source = source,
                Success = success,
                DaysImported = days,
                Message = message.Length > 2000 ? message[..2000] : message
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PlanImport log error: {ex.Message}");
        }
        try { StatusChanged?.Invoke(); } catch { }
    }

    private void SetStatus(string status)
    {
        Status = status;
        try { StatusChanged?.Invoke(); } catch { }
    }
}
