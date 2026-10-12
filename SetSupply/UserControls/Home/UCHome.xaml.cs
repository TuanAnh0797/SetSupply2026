using APP.ViewModels.UserControlViewModels.Home;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace APP.UserControls;

public partial class UCHome : UserControl
{
    private readonly UCHomeViewModel _viewModel;

    public UCHome()
    {
        InitializeComponent();
        _viewModel = App.ServiceProvider.GetRequiredService<UCHomeViewModel>();
        DataContext = _viewModel;

        Loaded += UCHome_Loaded;
        BodyScroll.ScrollChanged += (_, _) => AlignHeaderWithBody();
        BodyScroll.SizeChanged += (_, _) => AlignHeaderWithBody();
    }

    private async void UCHome_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.ActivateAsync();
    }

    /// <summary>
    /// Khi thân bảng có thanh cuộn dọc, chừa khoảng tương ứng ở header/footer để cột thẳng hàng.
    /// </summary>
    private void AlignHeaderWithBody()
    {
        double scrollBarWidth = 0;
        if (BodyScroll.ComputedVerticalScrollBarVisibility == Visibility.Visible &&
            BodyScroll.Template?.FindName("PART_VerticalScrollBar", BodyScroll) is ScrollBar bar)
            scrollBarWidth = bar.ActualWidth;

        var margin = new Thickness(0, 0, scrollBarWidth, 0);
        HeaderGrid.Margin = margin;
        FooterGrid.Margin = margin;
        FlatHeaderGrid.Margin = margin;
        FlatFooterGrid.Margin = margin;
        PlanRowGrid.Margin = margin;
        FlatPlanRowGrid.Margin = margin;
    }
}
