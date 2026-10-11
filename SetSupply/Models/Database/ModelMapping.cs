using System.ComponentModel.DataAnnotations;

namespace SetSupply.Models.Database;

/// <summary>
/// Gán 1 model vào 1 nhóm model (VD: NR-C33ES2-C thuộc nhóm NR-C33ES) + ảnh của model.
/// Model không có trong bảng này => tự vào nhóm "Khác".
/// Thứ tự hiển thị nhóm = SortOrder nhỏ nhất của các dòng trong nhóm.
/// </summary>
public class ModelMapping
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string GroupName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn ảnh: đường dẫn đầy đủ (VD D:\Anh\NR-C33ES2-C.png) hoặc tên file trong thư mục Resources\ModelImages.
    /// Để trống => dùng ảnh theo quy ước &lt;tên model&gt;.png / &lt;tên nhóm&gt;.png trong Resources\ModelImages.
    /// </summary>
    [MaxLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
