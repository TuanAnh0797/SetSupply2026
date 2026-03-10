using APP.Database;
using Microsoft.EntityFrameworkCore;
using SetSupply.Models.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetSupply.Service
{
    public class ShiftSettingService
    {
        private readonly AppDbContext _db;

        public ShiftSettingService(AppDbContext db)
        {
            _db = db;
        }

        // ===== LẤY CÀI ĐẶT =====

        public async Task<List<ShiftSetting>> GetAllShiftSettingsAsync()
        {
            return await _db.ShiftSettings
                .OrderBy(s => s.ShiftNumber)
                .ToListAsync();
        }

        public async Task<List<ShiftSetting>> GetActiveShiftSettingsAsync()
        {
            return await _db.ShiftSettings
                .Where(s => s.IsUse)
                .OrderBy(s => s.ShiftNumber)
                .ToListAsync();
        }

        public async Task<ShiftSetting?> GetShiftSettingAsync(int shiftNumber)
        {
            return await _db.ShiftSettings
                .FirstOrDefaultAsync(s => s.ShiftNumber == shiftNumber);
        }

        // ===== CẬP NHẬT CÀI ĐẶT (OVERLOADS) =====

        public async Task<bool> UpdateShiftSettingAsync(int shiftNumber, TimeSpan startTime, TimeSpan endTime)
        {
            return await UpdateShiftSettingAsync(shiftNumber, startTime, endTime, true, DateTime.Today);
        }

        public async Task<bool> UpdateShiftSettingAsync(int shiftNumber, TimeSpan startTime, TimeSpan endTime, DateTime dateUsing)
        {
            return await UpdateShiftSettingAsync(shiftNumber, startTime, endTime, true, dateUsing);
        }

        public async Task<bool> UpdateShiftSettingAsync(int shiftNumber, TimeSpan startTime, TimeSpan endTime, bool isUse)
        {
            return await UpdateShiftSettingAsync(shiftNumber, startTime, endTime, isUse, DateTime.Today);
        }

        /// <summary>
        /// Cập nhật cài đặt ca + UPSERT history (mỗi ngày + ca chỉ 1 record).
        /// Nếu history chưa có cho ngày đó + ca đó => thêm mới.
        /// Nếu đã có => update.
        /// </summary>
        public async Task<bool> UpdateShiftSettingAsync(int shiftNumber, TimeSpan startTime, TimeSpan endTime, bool isUse, DateTime dateUsing)
        {
            try
            {
                var setting = await _db.ShiftSettings
                    .FirstOrDefaultAsync(s => s.ShiftNumber == shiftNumber);

                if (setting == null) return false;

                // UPSERT history: tìm record theo ngày + ca
                var existingHistory = await _db.ShiftSettingHistory
                    .FirstOrDefaultAsync(h => h.DateUsing.Date == dateUsing.Date && h.ShiftNumber == shiftNumber);

                if (existingHistory != null)
                {
                    // Đã có => UPDATE
                    existingHistory.StartTime = setting.StartTime;
                    existingHistory.EndTime = setting.EndTime;
                    existingHistory.IsUse = setting.IsUse;
                    existingHistory.InsertTime = DateTime.Now;
                }
                else
                {
                    // Chưa có => THÊM MỚI
                    _db.ShiftSettingHistory.Add(new ShiftSettingHistory
                    {
                        DateUsing = dateUsing.Date,
                        ShiftNumber = setting.ShiftNumber,
                        StartTime = setting.StartTime,
                        EndTime = setting.EndTime,
                        IsUse = setting.IsUse,
                        InsertTime = DateTime.Now
                    });
                }

                // Cập nhật cài đặt mới
                setting.StartTime = startTime;
                setting.EndTime = endTime;
                setting.IsUse = isUse;
                setting.UpdateTime = DateTime.Now;

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating shift setting: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ToggleShiftUseAsync(int shiftNumber, bool isUse)
        {
            try
            {
                var setting = await _db.ShiftSettings
                    .FirstOrDefaultAsync(s => s.ShiftNumber == shiftNumber);

                if (setting == null) return false;

                setting.IsUse = isUse;
                setting.UpdateTime = DateTime.Now;
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling shift use: {ex.Message}");
                return false;
            }
        }

        // ===== XEM LỊCH SỬ =====

        public async Task<List<ShiftSettingHistory>> GetHistoryByDateAsync(DateTime date)
        {
            return await _db.ShiftSettingHistory
                .Where(h => h.DateUsing.Date == date.Date)
                .OrderBy(h => h.ShiftNumber)
                .ToListAsync();
        }

        public async Task<List<ShiftSettingHistory>> GetHistoryByShiftAsync(int shiftNumber)
        {
            return await _db.ShiftSettingHistory
                .Where(h => h.ShiftNumber == shiftNumber)
                .OrderByDescending(h => h.DateUsing)
                .ThenByDescending(h => h.InsertTime)
                .ToListAsync();
        }

        public async Task<List<ShiftSettingHistory>> GetHistoryByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _db.ShiftSettingHistory
                .Where(h => h.DateUsing.Date >= fromDate.Date && h.DateUsing.Date <= toDate.Date)
                .OrderByDescending(h => h.DateUsing)
                .ThenBy(h => h.ShiftNumber)
                .ToListAsync();
        }

        public async Task<List<ShiftSettingHistory>> GetHistoryByMonthAsync(int year, int month)
        {
            return await _db.ShiftSettingHistory
                .Where(h => h.DateUsing.Year == year && h.DateUsing.Month == month)
                .OrderByDescending(h => h.DateUsing)
                .ThenBy(h => h.ShiftNumber)
                .ToListAsync();
        }

        // ===== UTILITY =====

        public async Task<bool> HasHistoryForDateAsync(DateTime date)
        {
            return await _db.ShiftSettingHistory
                .AnyAsync(h => h.DateUsing.Date == date.Date);
        }

        /// <summary>
        /// Lưu snapshot cài đặt hiện tại vào lịch sử — UPSERT.
        /// </summary>
        public async Task<bool> SaveCurrentSettingsToHistoryAsync(DateTime dateUsing)
        {
            try
            {
                var currentSettings = await GetAllShiftSettingsAsync();

                foreach (var setting in currentSettings)
                {
                    var existing = await _db.ShiftSettingHistory
                        .FirstOrDefaultAsync(h => h.DateUsing.Date == dateUsing.Date && h.ShiftNumber == setting.ShiftNumber);

                    if (existing != null)
                    {
                        existing.StartTime = setting.StartTime;
                        existing.EndTime = setting.EndTime;
                        existing.IsUse = setting.IsUse;
                        existing.InsertTime = DateTime.Now;
                    }
                    else
                    {
                        _db.ShiftSettingHistory.Add(new ShiftSettingHistory
                        {
                            DateUsing = dateUsing.Date,
                            ShiftNumber = setting.ShiftNumber,
                            StartTime = setting.StartTime,
                            EndTime = setting.EndTime,
                            IsUse = setting.IsUse,
                            InsertTime = DateTime.Now
                        });
                    }
                }

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings to history: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ReloadSettingsAsync()
        {
            try
            {
                await _db.ShiftSettings.LoadAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reloading settings: {ex.Message}");
                return false;
            }
        }
    }
}