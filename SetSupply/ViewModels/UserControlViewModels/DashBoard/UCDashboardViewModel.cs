using APP.Database;
using APP.Service;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SetSupply.Models.Home;
using SetSupply.Service;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SetSupply.ViewModels.UserControlViewModels.DashBoard
{
    public partial class UCDashboardViewModel : ObservableObject
    {
        private readonly AppDbContext _db;
        private readonly ShiftSettingService _shiftService;
        private readonly ProductionDataService _productionService;

        private readonly PLCService _plcService;

        //private DispatcherTimer _timer;
        private bool _isInitialized;

        private ShiftSettings _shift1Settings;
        private ShiftSettings _shift2Settings;
        private ShiftSettings _shift3Settings;

        // ===== SUMMARY CARDS =====

        [ObservableProperty]
        private int todayTotalOutput;

        [ObservableProperty]
        private int shift1Output;

        [ObservableProperty]
        private int shift2Output;

        [ObservableProperty]
        private int shift3Output;

        [ObservableProperty]
        private int currentHourOutput;

        [ObservableProperty]
        private string currentShiftName = "—";

        [ObservableProperty]
        private string lastUpdateTime;

        // ===== BAR CHART =====

        [ObservableProperty]
        private ISeries[] shiftComparisonSeries;

        [ObservableProperty]
        private Axis[] shiftComparisonXAxes;

        [ObservableProperty]
        private Axis[] shiftComparisonYAxes;

        // ===== LINE CHART =====

        [ObservableProperty]
        private ISeries[] hourlyOutputSeries;

        [ObservableProperty]
        private Axis[] hourlyOutputXAxes;

        [ObservableProperty]
        private Axis[] hourlyOutputYAxes;

        public UCDashboardViewModel(AppDbContext db, ShiftSettingService shiftService, ProductionDataService productionService, PLCService pLCService)
        {
            _db = db;
            _shiftService = shiftService;
            _productionService = productionService;
            _plcService = pLCService;

            _plcService.DataReceived += _plcService_DataReceived;
            InitializeCharts();
        }

        private void _plcService_DataReceived(string obj)
        {
            // Sự kiện đến từ thread PLC; DbContext dùng chung chỉ nên truy cập trên UI thread
            Application.Current?.Dispatcher.InvokeAsync(async () =>
            {
                await LoadShiftSettingsAsync();
                await LoadDashboardDataAsync();
            });
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;
            await LoadShiftSettingsAsync();
            await LoadDashboardDataAsync();
            
            _isInitialized = true;
        }

        private void InitializeCharts()
        {
            ShiftComparisonXAxes = new Axis[]
            {
            new Axis
            {
                Labels = new[] { "Hôm Nay" },
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.DimGray)
            }
            };

            ShiftComparisonYAxes = new Axis[]
            {
            new Axis
            {
                TextSize = 10,
                LabelsPaint = new SolidColorPaint(SKColors.DimGray),
                MinLimit = 0
            }
            };

            ShiftComparisonSeries = new ISeries[]
            {
            new ColumnSeries<int>
            {
                Values = new[] { 0 },
                Name = "CA 1",
                MaxBarWidth = 40,
                Padding = 4,
                Fill = new SolidColorPaint(new SKColor(0x00, 0x79, 0x6B))
            },
            new ColumnSeries<int>
            {
                Values = new[] { 0 },
                Name = "CA 2",
                MaxBarWidth = 40,
                Padding = 4,
                Fill = new SolidColorPaint(new SKColor(0xFF, 0x6F, 0x00))
            },
            new ColumnSeries<int>
            {
                Values = new[] { 0 },
                Name = "CA 3",
                MaxBarWidth = 40,
                Padding = 4,
                Fill = new SolidColorPaint(new SKColor(0x30, 0x3F, 0x9F))
            }
            };

            HourlyOutputXAxes = new Axis[]
            {
            new Axis
            {
                TextSize = 9,
                LabelsPaint = new SolidColorPaint(SKColors.DimGray),
                LabelsRotation = -45
            }
            };

            HourlyOutputYAxes = new Axis[]
            {
            new Axis
            {
                TextSize = 10,
                LabelsPaint = new SolidColorPaint(SKColors.DimGray),
                MinLimit = 0
            }
            };

            HourlyOutputSeries = Array.Empty<ISeries>();
        }

        private async Task LoadShiftSettingsAsync()
        {
            try
            {
                var settings = await _shiftService.GetAllShiftSettingsAsync();
                if (settings != null && settings.Count >= 3)
                {
                    _shift1Settings = ToLocal(settings[0]);
                    _shift2Settings = ToLocal(settings[1]);
                    _shift3Settings = ToLocal(settings[2]);
                }
                else
                {
                    ApplyDefaults();
                }
            }
            catch { ApplyDefaults(); }
        }

        private static ShiftSettings ToLocal(SetSupply.Models.Database.ShiftSetting s) => new ShiftSettings
        {
            ShiftNumber = s.ShiftNumber,
            StartHour = s.StartTime.Hours,
            EndHour = s.EndTime.Hours,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            TargetPerHour = 100
        };

        private void ApplyDefaults()
        {
            _shift1Settings = new ShiftSettings { ShiftNumber = 1, StartHour = 6, EndHour = 14, StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(14, 0, 0), TargetPerHour = 100 };
            _shift2Settings = new ShiftSettings { ShiftNumber = 2, StartHour = 14, EndHour = 22, StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(22, 0, 0), TargetPerHour = 100 };
            _shift3Settings = new ShiftSettings { ShiftNumber = 3, StartHour = 22, EndHour = 6, StartTime = new TimeSpan(22, 0, 0), EndTime = new TimeSpan(6, 0, 0), TargetPerHour = 100 };
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                var today = DateTime.Today;

                var s1 = await _productionService.GetTotalProductionByDateAndShiftAsync(today, 1);
                var s2 = await _productionService.GetTotalProductionByDateAndShiftAsync(today, 2);
                var s3 = await _productionService.GetTotalProductionByDateAndShiftAsync(today, 3);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Shift1Output = s1;
                    Shift2Output = s2;
                    Shift3Output = s3;
                    TodayTotalOutput = s1 + s2 + s3;
                    LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");

                    UpdateCurrentShiftInfo();
                    UpdateBarChart(s1, s2, s3);
                });

                await LoadHourlyChartAsync(today);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard error: {ex.Message}");
            }
        }

        private void UpdateCurrentShiftInfo()
        {
            var now = DateTime.Now.TimeOfDay;
            var all = new[] { _shift1Settings, _shift2Settings, _shift3Settings };
            var names = new[] { "CA 1", "CA 2", "CA 3" };

            for (int i = 0; i < all.Length; i++)
            {
                var s = all[i];
                if (s == null) continue;
                bool inShift = s.EndTime > s.StartTime
                    ? now >= s.StartTime && now < s.EndTime
                    : now >= s.StartTime || now < s.EndTime;
                if (inShift)
                {
                    CurrentShiftName = $"{names[i]} ({s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm})";
                    return;
                }
            }
            CurrentShiftName = "Ngoài ca";
        }

        private void UpdateBarChart(int s1, int s2, int s3)
        {
            ShiftComparisonSeries = new ISeries[]
            {
            new ColumnSeries<int> { Values = new[] { s1 }, Name = "CA 1", MaxBarWidth = 40, Padding = 4, Fill = new SolidColorPaint(new SKColor(0x00, 0x79, 0x6B)) },
            new ColumnSeries<int> { Values = new[] { s2 }, Name = "CA 2", MaxBarWidth = 40, Padding = 4, Fill = new SolidColorPaint(new SKColor(0xFF, 0x6F, 0x00)) },
            new ColumnSeries<int> { Values = new[] { s3 }, Name = "CA 3", MaxBarWidth = 40, Padding = 4, Fill = new SolidColorPaint(new SKColor(0x30, 0x3F, 0x9F)) }
            };
        }

        private async Task LoadHourlyChartAsync(DateTime date)
        {
            try
            {
                var stats = await _productionService.GetProductionStatsByHourAsync(date);

                var allHours = new List<int>();
                foreach (var s in new[] { _shift1Settings, _shift2Settings, _shift3Settings })
                {
                    if (s == null) continue;
                    allHours.AddRange(GetHours(s));
                }
                allHours = allHours.Distinct().ToList();

                int startH = _shift1Settings?.StartHour ?? 6;
                allHours = allHours.OrderBy(h => h < startH ? h + 24 : h).ToList();

                var labels = allHours.Select(h => $"{h:D2}:00").ToArray();
                var values = allHours.Select(h => stats.TryGetValue(h, out int v) ? v : 0).ToList();

                CurrentHourOutput = stats.TryGetValue(DateTime.Now.Hour, out int cv) ? cv : 0;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    HourlyOutputSeries = new ISeries[]
                    {
                    new LineSeries<int>
                    {
                        Values = values,
                        Name = "Sản lượng/giờ",
                        Stroke = new SolidColorPaint(new SKColor(0x42, 0xA5, 0xF5), 2.5f),
                        Fill = new SolidColorPaint(new SKColor(0x42, 0xA5, 0xF5, 40)),
                        GeometrySize = 5,
                        GeometryStroke = new SolidColorPaint(new SKColor(0x42, 0xA5, 0xF5), 2),
                        GeometryFill = new SolidColorPaint(SKColors.White),
                        LineSmoothness = 0.3
                    }
                    };

                    HourlyOutputXAxes = new Axis[]
                    {
                    new Axis
                    {
                        Labels = labels,
                        TextSize = 9,
                        LabelsPaint = new SolidColorPaint(SKColors.DimGray),
                        LabelsRotation = -45
                    }
                    };
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hourly chart error: {ex.Message}");
            }
        }

        private static List<int> GetHours(ShiftSettings s)
        {
            var h = new List<int>();
            if (s.EndHour > s.StartHour)
                for (int i = s.StartHour; i < s.EndHour; i++) h.Add(i);
            else
            {
                for (int i = s.StartHour; i < 24; i++) h.Add(i);
                for (int i = 0; i < s.EndHour; i++) h.Add(i);
            }
            return h;
        }

       

      
    }
}
