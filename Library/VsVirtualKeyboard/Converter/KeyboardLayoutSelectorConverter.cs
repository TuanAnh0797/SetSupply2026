using System.Globalization;
using System.Windows;
using System.Windows.Data;
using VsVirtualKeyboard.Const;

namespace VsVirtualKeyboard.Converter;

public class KeyboardLayoutSelectorConverter : IValueConverter
{
    public DataTemplate? DefaultTemplate { get; set; }
    public DataTemplate? NumPadTemplate { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not eVkLayout layout)
            return DefaultTemplate;

        return layout switch
        {
            eVkLayout.Numeric => NumPadTemplate,
            eVkLayout.Decimal => NumPadTemplate,
            _ => DefaultTemplate,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
