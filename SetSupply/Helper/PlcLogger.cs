using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace APP.Helper;

/// <summary>
/// Log giao tiếp PLC: mỗi ngày 1 file "Logs\PLC_yyyyMMdd.log" cạnh exe, giữ 30 ngày.
/// Cùng 1 lỗi lặp lại chỉ ghi tối đa 1 lần / 5 phút (tránh phình file khi mất kết nối lâu).
/// </summary>
public static class PlcLogger
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, DateTime> LastLogged = new();
    private static readonly TimeSpan RepeatInterval = TimeSpan.FromMinutes(5);
    private const int KeepDays = 30;
    private static DateTime _lastCleanup = DateTime.MinValue;

    public static string LogFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

    public static void Info(string message) => Write("INFO ", message);

    public static void Warn(string message) => Write("WARN ", message);

    public static void Error(string message) => Write("ERROR", message);

    /// <summary>
    /// Ghi lỗi nhưng bỏ qua nếu cùng nội dung đã ghi trong 5 phút gần đây.
    /// </summary>
    public static void ErrorThrottled(string message)
    {
        lock (Sync)
        {
            var now = DateTime.Now;
            if (LastLogged.TryGetValue(message, out var last) && now - last < RepeatInterval)
                return;
            LastLogged[message] = now;
        }
        Error(message);
    }

    /// <summary>
    /// Gọi khi kết nối lại thành công để lỗi kế tiếp được ghi ngay.
    /// </summary>
    public static void ResetThrottle()
    {
        lock (Sync) LastLogged.Clear();
    }

    private static void Write(string level, string message)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(LogFolder);
                var now = DateTime.Now;
                var path = Path.Combine(LogFolder, $"PLC_{now:yyyyMMdd}.log");
                File.AppendAllText(path, $"[{now:yyyy-MM-dd HH:mm:ss.fff}] {level} {message}{Environment.NewLine}", Encoding.UTF8);

                if (now - _lastCleanup > TimeSpan.FromHours(6))
                {
                    _lastCleanup = now;
                    foreach (var file in Directory.GetFiles(LogFolder, "PLC_*.log")
                                 .Where(f => File.GetLastWriteTime(f) < now.AddDays(-KeepDays)))
                        File.Delete(file);
                }
            }
        }
        catch
        {
            // Không để lỗi ghi log làm dừng vòng giao tiếp PLC
        }
    }
}
