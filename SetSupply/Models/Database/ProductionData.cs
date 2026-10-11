using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SetSupply.Models.Database;

public class ProductionData
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Codeback { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Serial { get; set; } = string.Empty;

    [Required]
    public DateTime DateTimeInsert { get; set; } = DateTime.Now;

    /// <summary>
    /// Dữ liệu test (tạo ở Cài đặt > Test Data hoặc giả lập PLC) => xóa riêng được, không ảnh hưởng dữ liệu thật.
    /// </summary>
    public bool IsTest { get; set; }

    // ===== PROPERTIES TÍNH TOÁN (KHÔNG LƯU VÀO DB) =====

    /// <summary>
    /// Lấy giờ từ DateTimeInsert
    /// </summary>
    [NotMapped]
    public int Hour => DateTimeInsert.Hour;

    /// <summary>
    /// Lấy ngày (không có giờ phút giây)
    /// </summary>
    [NotMapped]
    public DateTime Date => DateTimeInsert.Date;

    /// <summary>
    /// Format ngày giờ để hiển thị
    /// </summary>
    [NotMapped]
    public string DateTimeDisplay => DateTimeInsert.ToString("dd/MM/yyyy HH:mm:ss");

    /// <summary>
    /// Format ngày để hiển thị
    /// </summary>
    [NotMapped]
    public string DateDisplay => DateTimeInsert.ToString("dd/MM/yyyy");

    /// <summary>
    /// Format giờ để hiển thị
    /// </summary>
    [NotMapped]
    public string TimeDisplay => DateTimeInsert.ToString("HH:mm:ss");

    /// <summary>
    /// Lấy giờ với format "HH:00"
    /// </summary>
    [NotMapped]
    public string HourDisplay => $"{Hour:D2}:00";
}