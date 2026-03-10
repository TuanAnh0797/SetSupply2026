using CommunityToolkit.Mvvm.ComponentModel;
using SetSupply.Models.Database;
using SetSupply.Models.Home;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace SetSupply.ViewModels.UserControlViewModels.Home
{
    public partial class ShiftViewModel : ObservableObject
    {
        [ObservableProperty]
        private string shiftName;

        [ObservableProperty]
        private string timeRange;

        [ObservableProperty]
        private int totalOutput;

        [ObservableProperty]
        private int target;

        [ObservableProperty]
        private double achievement;

        [ObservableProperty]
        private int average;

        [ObservableProperty]
        private string maxHour = "00:00";

        [ObservableProperty]
        private int maxOutput;

        [ObservableProperty]
        private string minHour = "00:00";

        [ObservableProperty]
        private int minOutput;

        [ObservableProperty]
        private ObservableCollection<ProductionRowData> data;

        public ShiftSettings Settings { get; private set; }

        public int ShiftNumber { get; }

        public ShiftViewModel(int shiftNumber, string defaultName)
        {
            ShiftNumber = shiftNumber;
            ShiftName = defaultName;
            Data = new ObservableCollection<ProductionRowData>();
        }

        public void ApplySettings(ShiftSettings settings)
        {
            Settings = settings;
            TimeRange = $"{settings.StartTime:hh\\:mm} - {settings.EndTime:hh\\:mm}";
        }

        /// <summary>
        /// Lấy danh sách giờ theo ca (hỗ trợ ca qua đêm)
        /// </summary>
        public List<int> GetShiftHours()
        {
            var hours = new List<int>();
            if (Settings == null) return hours;

            if (Settings.EndHour > Settings.StartHour)
            {
                for (int h = Settings.StartHour; h < Settings.EndHour; h++)
                    hours.Add(h);
            }
            else
            {
                for (int h = Settings.StartHour; h < 24; h++)
                    hours.Add(h);
                for (int h = 0; h < Settings.EndHour; h++)
                    hours.Add(h);
            }

            return hours;
        }

        /// <summary>
        /// Cập nhật dữ liệu sản xuất từ ma trận database
        /// </summary>
        public void UpdateProductionData(Dictionary<string, Dictionary<int, int>> matrix)
        {
            var shiftHours = GetShiftHours();

            Data.Clear();

            int stt = 1;
            var hourlyTotals = shiftHours.ToDictionary(h => h, _ => 0);

            foreach (var modelData in matrix)
            {
                var rowData = new ProductionRowData
                {
                    STT = stt++,
                    ModelName = modelData.Key,
                    HourlyOutputs = new ObservableCollection<int>(),
                    Total = 0,
                    Note = ""
                };

                foreach (int hour in shiftHours)
                {
                    int count = modelData.Value.TryGetValue(hour, out int val) ? val : 0;
                    rowData.HourlyOutputs.Add(count);
                    hourlyTotals[hour] += count;
                }

                rowData.Total = rowData.HourlyOutputs.Sum();
                Data.Add(rowData);
            }

            if (Data.Count > 0)
            {
                var totalRow = new ProductionRowData
                {
                    STT = 0,
                    ModelName = "TỔNG",
                    HourlyOutputs = new ObservableCollection<int>(),
                    Total = 0,
                    Note = ""
                };

                int grandTotal = 0;
                foreach (int hour in shiftHours)
                {
                    int hourTotal = hourlyTotals.TryGetValue(hour, out int val) ? val : 0;
                    totalRow.HourlyOutputs.Add(hourTotal);
                    grandTotal += hourTotal;
                }

                totalRow.Total = grandTotal;
                Data.Add(totalRow);
            }

            UpdateSummary();
        }

        private void UpdateSummary()
        {
            var dataWithoutTotal = Data.Where(x => x.ModelName != "TỔNG").ToList();

            TotalOutput = dataWithoutTotal.Sum(x => x.Total);

            int hours = dataWithoutTotal.Count > 0 && dataWithoutTotal[0].HourlyOutputs.Count > 0
                ? dataWithoutTotal[0].HourlyOutputs.Count
                : 0;

            Average = hours > 0 ? TotalOutput / hours : 0;

            string localMaxHour = "00:00";
            int localMaxOutput = 0;
            string localMinHour = "00:00";
            int localMinOutput = int.MaxValue;

            if (dataWithoutTotal.Count > 0 && dataWithoutTotal[0].HourlyOutputs.Count > 0)
            {
                var shiftHours = GetShiftHours();

                for (int i = 0; i < dataWithoutTotal[0].HourlyOutputs.Count && i < shiftHours.Count; i++)
                {
                    int hourTotal = dataWithoutTotal.Sum(x => i < x.HourlyOutputs.Count ? x.HourlyOutputs[i] : 0);

                    if (hourTotal > localMaxOutput)
                    {
                        localMaxOutput = hourTotal;
                        localMaxHour = $"{shiftHours[i]:D2}:00";
                    }

                    if (hourTotal < localMinOutput && hourTotal > 0)
                    {
                        localMinOutput = hourTotal;
                        localMinHour = $"{shiftHours[i]:D2}:00";
                    }
                }
            }

            MaxHour = localMaxHour;
            MaxOutput = localMaxOutput;
            MinHour = localMinHour;
            MinOutput = localMinOutput == int.MaxValue ? 0 : localMinOutput;
        }
    }
}