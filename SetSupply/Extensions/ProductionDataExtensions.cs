using SetSupply.Models.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SetSupply.Extensions;

public static class ProductionDataExtensions
{
    /// <summary>
    /// Xác định ca làm việc dựa trên cài đặt ShiftSetting
    /// </summary>
    public static int GetShiftNumber(this ProductionData data, List<ShiftSetting> shiftSettings)
    {
        if (shiftSettings == null || !shiftSettings.Any())
            return 1; // Mặc định ca 1

        var timeOfDay = data.DateTimeInsert.TimeOfDay;

        foreach (var shift in shiftSettings.Where(s => s.IsUse).OrderBy(s => s.ShiftNumber))
        {
            if (IsInShift(timeOfDay, shift.StartTime, shift.EndTime))
            {
                return shift.ShiftNumber;
            }
        }

        return 1; // Mặc định ca 1 nếu không tìm thấy
    }

    /// <summary>
    /// Lấy tên ca dựa trên cài đặt
    /// </summary>
    public static string GetShiftName(this ProductionData data, List<ShiftSetting> shiftSettings)
    {
        var shiftNumber = data.GetShiftNumber(shiftSettings);

        var shift = shiftSettings.FirstOrDefault(s => s.ShiftNumber == shiftNumber);

        if (shift != null)
        {
            return shiftNumber switch
            {
                1 => "CA 1 - SÁNG",
                2 => "CA 2 - CHIỀU",
                3 => "CA 3 - ĐÊM",
                _ => $"CA {shiftNumber}"
            };
        }

        return "KHÔNG XÁC ĐỊNH";
    }

    /// <summary>
    /// Kiểm tra thời gian có nằm trong ca không (hỗ trợ ca qua đêm)
    /// </summary>
    private static bool IsInShift(TimeSpan currentTime, TimeSpan startTime, TimeSpan endTime)
    {
        if (endTime > startTime)
        {
            // Ca bình thường (ví dụ: 6:00 - 14:00)
            return currentTime >= startTime && currentTime < endTime;
        }
        else
        {
            // Ca qua đêm (ví dụ: 22:00 - 6:00)
            return currentTime >= startTime || currentTime < endTime;
        }
    }

    /// <summary>
    /// Filter danh sách ProductionData theo ca
    /// </summary>
    public static List<ProductionData> FilterByShift(this List<ProductionData> dataList, int shiftNumber, List<ShiftSetting> shiftSettings)
    {
        return dataList.Where(d => d.GetShiftNumber(shiftSettings) == shiftNumber).ToList();
    }
}