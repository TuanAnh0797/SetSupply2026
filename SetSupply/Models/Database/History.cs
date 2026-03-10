using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.Models.Database;

public  class History
{
    [NotMapped]
    public int? STT { get; set; }
    public int? Id { get; set; }
    public string? Shift { get; set; } = "";
    public string? Mold { get; set; } = "";
    public string? ModelName { get; set; } = "";
    public string? MaterialName {  get; set; } = "";
    public string? MaterialCode { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public string? MaterialColor { get; set; } = "";
    public string? NameError {  get; set; } = "";
    public string? Position { get; set; } = "";
    public string? Persion { get; set; } = "";
    public string? PositionError { get; set; } = "";
    public string? Reason { get; set; } = "";
    public string? Action { get; set; } = "";
    public DateTime TimeInsert { get; set; } = DateTime.Now;
    public DateTime? TimeUpdate { get; set; }


}
