using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetSupply.Models.Home
{
    /// <summary>
    /// Cài đặt ca làm việc
    /// </summary>
    public class ShiftSettings
    {
        public int ShiftNumber { get; set; }
        public int StartHour { get; set; }
        public int EndHour { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int TargetPerHour { get; set; }
    }

    /// <summary>
    /// Dữ liệu sản lượng theo dòng (mỗi model)
    /// </summary>
    public partial class ProductionRowData : ObservableObject
    {
        [ObservableProperty]
        private int sTT;

        [ObservableProperty]
        private string modelName;

        [ObservableProperty]
        private ObservableCollection<int> hourlyOutputs;

        [ObservableProperty]
        private int total;

        [ObservableProperty]
        private string note;
    }
}
