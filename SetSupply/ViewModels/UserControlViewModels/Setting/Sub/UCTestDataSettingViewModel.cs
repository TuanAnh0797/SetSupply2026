using APP.Service;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SetSupply.Service;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SetSupply.ViewModels.UserControlViewModels.Setting.Sub;

/// <summary>
/// Dữ liệu test: tạo hàng loạt, giả lập PLC gửi mã, xóa dữ liệu test (chỉ bản ghi IsTest, không đụng dữ liệu thật).
/// </summary>
public partial class UCTestDataSettingViewModel : ObservableObject
{
    private readonly TestDataService _testDataService;
    private readonly ProductionDataService _productionService;
    private readonly ModelGroupService _modelGroupService;
    private readonly PLCService _plcService;
    private readonly DispatcherTimer _autoSendTimer = new();
    private readonly Random _random = new();
    private int _autoSentCount;

    // ===== Tạo hàng loạt =====
    [ObservableProperty] private DateTime fromDate = DateTime.Today;
    [ObservableProperty] private DateTime toDate = DateTime.Today;
    [ObservableProperty] private string modelsText = string.Empty;
    [ObservableProperty] private int minPerHour = 3;
    [ObservableProperty] private int maxPerHour = 8;
    [ObservableProperty] private bool onlyUntilNow = true;

    // ===== Giả lập PLC =====
    [ObservableProperty] private string simulateCode = string.Empty;
    [ObservableProperty] private int autoSendSeconds = 5;
    [ObservableProperty] private bool isAutoSending;
    [ObservableProperty] private string simulateResult = string.Empty;

    // ===== Trạng thái =====
    [ObservableProperty] private string testCountText = string.Empty;

    public UCTestDataSettingViewModel(TestDataService testDataService, ProductionDataService productionService,
                                      ModelGroupService modelGroupService, PLCService plcService)
    {
        _testDataService = testDataService;
        _productionService = productionService;
        _modelGroupService = modelGroupService;
        _plcService = plcService;
        _autoSendTimer.Tick += async (_, _) => await AutoSendTickAsync();
    }

    public async Task Load()
    {
        if (string.IsNullOrWhiteSpace(ModelsText))
            await LoadModels();
        await RefreshCount();
        if (string.IsNullOrWhiteSpace(SimulateCode))
            await NewCode();
    }

    /// <summary>
    /// Lấy danh sách model từ Model Group (chưa có thì lấy model đã sản xuất 30 ngày).
    /// </summary>
    [RelayCommand]
    private async Task LoadModels()
    {
        var models = (await _modelGroupService.GetMappingsAsync()).Select(m => m.ModelName).ToList();
        if (models.Count == 0)
            models = await _modelGroupService.GetRecentModelsAsync(30);
        if (models.Count == 0)
            models = new List<string> { "NR-TEST00001", "NR-TEST00002", "NR-TEST00003" };
        ModelsText = string.Join(Environment.NewLine, models);
    }

    private List<string> GetModels() => (ModelsText ?? string.Empty)
        .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(m => m.Trim()).Where(m => m.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    [RelayCommand]
    private async Task RefreshCount()
    {
        try
        {
            int total = await _productionService.CountTestDataAsync();
            TestCountText = total == 0
                ? "Hiện không có dữ liệu test."
                : $"Đang có {total.ToString("#,0", CultureInfo.InvariantCulture)} bản ghi TEST trong database.";
        }
        catch (Exception ex)
        {
            TestCountText = $"Lỗi đếm dữ liệu test: {ex.Message}";
        }
    }

    // ===== TẠO HÀNG LOẠT =====

    [RelayCommand]
    private async Task Generate()
    {
        var models = GetModels();
        if (models.Count == 0)
        {
            MessageBox.Show("Nhập ít nhất 1 model (mỗi dòng 1 model).", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (ToDate < FromDate)
        {
            MessageBox.Show("Ngày kết thúc phải sau ngày bắt đầu.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MinPerHour < 0 || MaxPerHour < MinPerHour || MaxPerHour > 500)
        {
            MessageBox.Show("Số lượng mỗi giờ phải thỏa 0 <= min <= max <= 500.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                $"Tạo dữ liệu TEST ngày {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy} cho {models.Count} model, " +
                $"{MinPerHour}-{MaxPerHour} sp/giờ/model{(OnlyUntilNow ? ", chỉ tới giờ hiện tại" : "")}?\n\n" +
                "Dữ liệu test được đánh dấu riêng và xóa được bằng nút \"Xóa dữ liệu test\".",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            int count = await _testDataService.GenerateAsync(FromDate, ToDate, models, MinPerHour, MaxPerHour, OnlyUntilNow);
            await RefreshCount();
            await NewCode(); // serial đã tăng => lấy mã giả lập mới
            MessageBox.Show($"Đã tạo {count.ToString("#,0", CultureInfo.InvariantCulture)} bản ghi test.", "Thành công",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tạo dữ liệu test: {ex.GetBaseException().Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    // ===== GIẢ LẬP PLC =====

    [RelayCommand]
    private async Task NewCode()
    {
        var models = GetModels();
        var model = models.Count > 0 ? models[_random.Next(models.Count)] : "NR-TEST00001";
        SimulateCode = await _testDataService.NewCodeAsync(model);
    }

    /// <summary>
    /// Gửi mã như PLC: lưu database (đánh dấu test), ô Codeback và màn hình Home cập nhật như mã thật.
    /// </summary>
    [RelayCommand]
    private async Task SendCode()
    {
        var status = await _plcService.SimulateReadAsync(SimulateCode);
        SimulateResult = $"{DateTime.Now:HH:mm:ss}  {SimulateCode}  =>  {StatusText(status)}";
        await RefreshCount();
        if (status == PlcReadStatus.Saved) await NewCode(); // mã kế tiếp
    }

    [RelayCommand]
    private void ToggleAutoSend()
    {
        if (IsAutoSending)
        {
            _autoSendTimer.Stop();
            IsAutoSending = false;
            SimulateResult += $"   (đã dừng, gửi {_autoSentCount} mã)";
            return;
        }

        if (AutoSendSeconds < 1 || AutoSendSeconds > 3600)
        {
            MessageBox.Show("Chu kỳ gửi phải từ 1 đến 3600 giây.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _autoSentCount = 0;
        _autoSendTimer.Interval = TimeSpan.FromSeconds(AutoSendSeconds);
        _autoSendTimer.Start();
        IsAutoSending = true;
    }

    private async Task AutoSendTickAsync()
    {
        await NewCode();
        var status = await _plcService.SimulateReadAsync(SimulateCode);
        _autoSentCount++;
        SimulateResult = $"Tự động: đã gửi {_autoSentCount} mã. Mới nhất {DateTime.Now:HH:mm:ss}  {SimulateCode}  =>  {StatusText(status)}";
        await RefreshCount();
    }

    private static string StatusText(PlcReadStatus status) => status switch
    {
        PlcReadStatus.Saved => "Đã lưu",
        PlcReadStatus.Duplicate => "Trùng (bỏ qua)",
        PlcReadStatus.Invalid => "Sai mã (cần 12 ký tự model + 7 ký tự serial)",
        _ => "Lỗi lưu database"
    };

    // ===== XÓA =====

    /// <summary>
    /// Xóa dữ liệu test trong các ngày sản xuất FromDate..ToDate.
    /// </summary>
    [RelayCommand]
    private async Task DeleteRange()
    {
        var (start, _) = await _productionService.GetProductionRangeAsync(FromDate, null);
        var (_, end) = await _productionService.GetProductionRangeAsync(ToDate, null);
        int count = await _productionService.CountTestDataAsync(start, end);
        if (count == 0)
        {
            MessageBox.Show($"Không có dữ liệu test trong ngày {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show($"Xóa {count:#,0} bản ghi TEST của ngày sản xuất {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}?\n(Dữ liệu thật không bị ảnh hưởng.)",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        await DeleteAsync(start, end);
    }

    [RelayCommand]
    private async Task DeleteAll()
    {
        int count = await _productionService.CountTestDataAsync();
        if (count == 0)
        {
            MessageBox.Show("Không có dữ liệu test.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show($"Xóa TOÀN BỘ {count:#,0} bản ghi TEST?\n(Dữ liệu thật không bị ảnh hưởng.)",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        await DeleteAsync(null, null);
    }

    private async Task DeleteAsync(DateTime? from, DateTime? to)
    {
        try
        {
            int deleted = await _productionService.DeleteTestDataAsync(from, to);
            _testDataService.ResetSerial();
            await RefreshCount();
            MessageBox.Show($"Đã xóa {deleted.ToString("#,0", CultureInfo.InvariantCulture)} bản ghi test.", "Thành công",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi xóa: {ex.GetBaseException().Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
