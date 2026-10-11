using System.ComponentModel.DataAnnotations;

namespace SetSupply.Models.Database;

/// <summary>
/// Cài đặt chung dạng Key - Value (VD: ApplicationTitle = "SetSupply Monitor").
/// </summary>
public class AppSetting
{
    [Key]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Value { get; set; } = string.Empty;
}
