using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace SetSupply.Models.Database;

/// <summary>
/// Nhóm model (VD: NR-C33ES gồm NR-C33ES2-C, NR-C33ES2L-W...) + sản lượng kế hoạch theo ca.
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

    public int PlanShift1 { get; set; }
    public int PlanShift2 { get; set; }
    public int PlanShift3 { get; set; }

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

    public int GetPlan(int shiftNumber) => shiftNumber switch
    {
        1 => PlanShift1,
        2 => PlanShift2,
        3 => PlanShift3,
        _ => 0
    };
}
