using System;
using System.ComponentModel.DataAnnotations;

namespace SetSupply.Models.Database;

/// <summary>
/// Nhật ký import file kế hoạch. Dùng để không import lại file đã xử lý (cùng đường dẫn + kích thước + giờ sửa).
/// </summary>
public class PlanImportLog
{
    [Key]
    public int Id { get; set; }

    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime FileLastWrite { get; set; }

    public DateTime ImportedAt { get; set; } = DateTime.Now;

    /// <summary>"Tự động" hoặc "Thủ công".</summary>
    [MaxLength(20)]
    public string Source { get; set; } = string.Empty;

    public bool Success { get; set; }

    public int DaysImported { get; set; }

    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;
}
