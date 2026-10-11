using System;
using System.ComponentModel.DataAnnotations;

namespace SetSupply.Models.Database;

/// <summary>
/// Kế hoạch sản lượng của 1 nhóm model, 1 ca, trong 1 ngày sản xuất.
/// Ngày nào không có dòng ở đây thì dùng kế hoạch mặc định trong ModelGroup.PlanShiftN.
/// Lưu theo tên nhóm (ModelGroup bị ghi đè toàn bộ khi lưu cài đặt nên Id thay đổi).
/// </summary>
public class ProductionPlan
{
    [Key]
    public int Id { get; set; }

    public DateTime PlanDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string GroupName { get; set; } = string.Empty;

    public int ShiftNumber { get; set; }

    public int Quantity { get; set; }
}
