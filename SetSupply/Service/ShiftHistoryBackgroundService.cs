using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetSupply.Service
{
    public class ShiftHistoryBackgroundService
    {
        private readonly ShiftSettingService _shiftService;
        private readonly ShiftScheduleService _scheduleService;
        private Timer _timer;
        private bool _isRunning;

        // Chạy mỗi 30 phút
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

        public ShiftHistoryBackgroundService(ShiftSettingService shiftService, ShiftScheduleService scheduleService)
        {
            _shiftService = shiftService;
            _scheduleService = scheduleService;
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            // Chạy lần đầu sau 10 giây (đợi app khởi động xong), sau đó mỗi 2 tiếng
            _timer = new Timer(OnTimerElapsed, null, TimeSpan.FromSeconds(10), Interval);

            System.Diagnostics.Debug.WriteLine($"ShiftHistoryBackgroundService started. Interval: {Interval.TotalHours}h");
        }

        public void Stop()
        {
            _isRunning = false;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();
            _timer = null;

            System.Diagnostics.Debug.WriteLine("ShiftHistoryBackgroundService stopped.");
        }

        private async void OnTimerElapsed(object state)
        {
            try
            {
                // Bản chụp giờ ca + giờ nghỉ cho ngày sản xuất hiện tại (nếu chưa có)
                await _scheduleService.EnsureCurrentSnapshotAsync();

                var today = DateTime.Today;
                var result = await _shiftService.SaveCurrentSettingsToHistoryAsync(today);

                System.Diagnostics.Debug.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] ShiftHistory auto-save: {(result ? "OK" : "Failed")} for {today:dd/MM/yyyy}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShiftHistory auto-save error: {ex.Message}");
            }
        }
    }

}
