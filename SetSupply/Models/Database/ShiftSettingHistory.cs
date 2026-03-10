using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetSupply.Models.Database
{
    public class ShiftSettingHistory
    {
        [Key]
        public int Id { get; set; }
        public DateTime DateUsing { get; set; }
        [Required]
        public int ShiftNumber { get; set; } // 1, 2, 3

        [Required]
        public TimeSpan StartTime { get; set; } // Thời gian bắt đầu

        [Required]
        public TimeSpan EndTime { get; set; } // Thời gian kết thúc
        public bool IsUse { get; set; } = true; // THÊM CỘT NÀY
        public DateTime InsertTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Ca qua đêm: tự tính từ StartTime/EndTime, không lưu DB
        /// VD: StartTime=22:00, EndTime=06:00 => IsOvernight=true
        /// </summary>
        [NotMapped]
        public bool IsOvernight => EndTime <= StartTime;

    }
}
