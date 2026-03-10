using SetSupply.ViewModels.UserControlViewModels.Home;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace SetSupply.Helper
{
    public static class DataGridColumnHelper
    {
        public static void GenerateShiftColumns(DataGrid dataGrid, ShiftViewModel shift)
        {
            if (shift?.Settings == null || dataGrid == null) return;

            // 1. DETACH hoàn toàn: xóa ItemsSource binding và set null
            BindingOperations.ClearBinding(dataGrid, ItemsControl.ItemsSourceProperty);
            dataGrid.ItemsSource = null;
            dataGrid.Items.Refresh();

            // 2. Xóa cột cũ (an toàn vì DataGrid đã trống hoàn toàn)
            dataGrid.Columns.Clear();

            // 3. Force layout để DataGrid cập nhật visual tree
            dataGrid.UpdateLayout();

            List<int> hours = shift.GetShiftHours();

            // 4. Tạo columns mới
            // Cột STT
            dataGrid.Columns.Add(CreateStarColumn("STT", "STT", 0.5,
                HorizontalAlignment.Center));

            // Cột Model
            dataGrid.Columns.Add(CreateStarColumn("Model", "ModelName", 1.5,
                HorizontalAlignment.Center, FontWeights.SemiBold, 14.0));

            // Cột cho từng giờ
            for (int i = 0; i < hours.Count; i++)
            {
                dataGrid.Columns.Add(CreateStarColumn(
                    $"{hours[i]:D2}:00",
                    $"HourlyOutputs[{i}]",
                    1.0,
                    HorizontalAlignment.Center,
                    FontWeights.Bold,
                    14.0,
                    new SolidColorBrush(Color.FromRgb(0, 121, 107))
                ));
            }

            // Cột Tổng - NỔI BẬT với background và font lớn
            var totalColumn = new DataGridTextColumn
            {
                Header = "TỔNG",
                Binding = new Binding("Total"),
                Width = new DataGridLength(1.2, DataGridLengthUnitType.Star)
            };

            // Header style cho cột Tổng - nền xanh đậm hơn
            var totalHeaderStyle = new Style(typeof(DataGridColumnHeader));
            totalHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(Color.FromRgb(46, 125, 50)))); // #2E7D32
            totalHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, Brushes.White));
            totalHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.ExtraBold));
            totalHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.FontSizeProperty, 15.0));
            totalHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            totalHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            totalHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(10)));
            totalHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, Brushes.White));
            totalHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(2, 0, 0, 0)));

            // Custom template tránh lỗi FindAncestor
            var headerTemplate = new ControlTemplate(typeof(DataGridColumnHeader));
            var headerBorderFactory = new FrameworkElementFactory(typeof(Border));
            headerBorderFactory.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            headerBorderFactory.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            headerBorderFactory.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            headerBorderFactory.SetBinding(Border.PaddingProperty, new Binding("Padding") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            var headerContentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            headerContentFactory.SetBinding(ContentPresenter.HorizontalAlignmentProperty, new Binding("HorizontalContentAlignment") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            headerContentFactory.SetBinding(ContentPresenter.VerticalAlignmentProperty, new Binding("VerticalContentAlignment") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            headerBorderFactory.AppendChild(headerContentFactory);
            headerTemplate.VisualTree = headerBorderFactory;
            totalHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.TemplateProperty, headerTemplate));

            totalColumn.HeaderStyle = totalHeaderStyle;

            // Style cho cell trong cột Tổng
            var totalCellStyle = new Style(typeof(DataGridCell));
            totalCellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Color.FromRgb(232, 245, 233)))); // #E8F5E9
            totalCellStyle.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(200, 230, 201)))); // #C8E6C9
            totalCellStyle.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(2, 0, 0, 1)));
            totalCellStyle.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(8)));
            totalCellStyle.Setters.Add(new Setter(DataGridCell.FocusVisualStyleProperty, null));

            // Template cho cell
            var totalCellTemplate = new ControlTemplate(typeof(DataGridCell));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            borderFactory.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            borderFactory.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            borderFactory.SetBinding(Border.PaddingProperty, new Binding("Padding") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(TextElement.ForegroundProperty, new SolidColorBrush(Color.FromRgb(46, 125, 50))); // #2E7D32
            borderFactory.AppendChild(contentFactory);
            totalCellTemplate.VisualTree = borderFactory;
            totalCellStyle.Setters.Add(new Setter(DataGridCell.TemplateProperty, totalCellTemplate));

            // IsSelected trigger
            var selectedTrigger = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Color.FromRgb(200, 230, 201)))); // #C8E6C9
            totalCellStyle.Triggers.Add(selectedTrigger);

            totalColumn.CellStyle = totalCellStyle;

            // ElementStyle cho text
            var totalTextStyle = new Style(typeof(TextBlock));
            totalTextStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            totalTextStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            totalTextStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.ExtraBold));
            totalTextStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 16.0));
            totalTextStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(46, 125, 50)))); // #2E7D32
            totalColumn.ElementStyle = totalTextStyle;

            dataGrid.Columns.Add(totalColumn);

            // 5. GÁN LẠI ItemsSource bằng binding mới
            dataGrid.ItemsSource = shift.Data;
        }

        /// <summary>
        /// Xóa sạch DataGrid trước khi unload - tránh lỗi FindAncestor khi chuyển trang
        /// </summary>
        public static void ClearDataGrid(DataGrid dataGrid)
        {
            if (dataGrid == null) return;

            BindingOperations.ClearBinding(dataGrid, ItemsControl.ItemsSourceProperty);
            dataGrid.ItemsSource = null;
            dataGrid.Columns.Clear();
        }

        /// <summary>
        /// Tạo cột với Star width để tự co dãn fill hết DataGrid
        /// </summary>
        private static DataGridTextColumn CreateStarColumn(
            string header,
            string bindingPath,
            double starWeight,
            HorizontalAlignment alignment,
            FontWeight? fontWeight = null,
            double? fontSize = null,
            Brush foreground = null)
        {
            var column = new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding(bindingPath),
                Width = new DataGridLength(starWeight, DataGridLengthUnitType.Star)
            };

            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, alignment));
            style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));

            if (fontWeight.HasValue)
                style.Setters.Add(new Setter(TextBlock.FontWeightProperty, fontWeight.Value));

            if (fontSize.HasValue)
                style.Setters.Add(new Setter(TextBlock.FontSizeProperty, fontSize.Value));

            if (foreground != null)
                style.Setters.Add(new Setter(TextBlock.ForegroundProperty, foreground));

            column.ElementStyle = style;
            return column;
        }
    }
}
