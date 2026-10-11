using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace SetSupply.Models.Database;

/// <summary>
/// Nhóm model (VD: NR-C33ES gồm NR-C33ES2-C, NR-C33ES2L-W...). Chỉ dùng để gom model,
/// kế hoạch sản lượng nằm ở bảng ProductionPlans (Daily Plan).
/// Prefixes rỗng => nhóm "Khác" (gom tất cả model không khớp nhóm nào).
/// </summary>
public class ModelGroup
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách tiền tố model, cách nhau bởi dấu phẩy. VD: "NR-C33ES" hoặc "NR-C33ES, NR-C33EX"
    /// </summary>
    [MaxLength(500)]
    public string Prefixes { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    [NotMapped]
    public bool IsCatchAll => !GetPrefixes().Any();

    public IEnumerable<string> GetPrefixes()
    {
        return (Prefixes ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0);
    }
}
