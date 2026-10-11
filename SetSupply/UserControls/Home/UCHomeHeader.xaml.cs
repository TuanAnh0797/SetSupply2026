using APP.ViewModels.UserControlViewModels.Home;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace APP.UserControls;

/// <summary>
/// Phần điều khiển của Home nằm trên thanh tiêu đề chính (ngày, ca, cài đặt). Dùng chung UCHomeViewModel.
/// </summary>
public partial class UCHomeHeader : UserControl
{
    public UCHomeHeader()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<UCHomeViewModel>();
    }

    // Calendar giữ mouse capture sau khi chọn ngày => phải click 2 lần mới bấm được nút khác
    private void Calendar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (Mouse.Captured is CalendarItem || Mouse.Captured is CalendarDayButton)
            Mouse.Capture(null);
    }
}
