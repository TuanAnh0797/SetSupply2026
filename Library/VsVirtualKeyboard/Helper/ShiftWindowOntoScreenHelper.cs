using System.Windows;

namespace VsVirtualKeyboard.Helper;

public static class ShiftWindowOntoScreenHelper
{
    public static void ShiftWindowOntoScreen(Window window)
    {
        var wa = SystemParameters.WorkArea;

        double width = window.Width;
        double height = window.Height;

        if (double.IsNaN(width) || width == 0) width = window.ActualWidth;
        if (double.IsNaN(height) || height == 0) height = window.ActualHeight;

        double left = window.Left;
        double top = window.Top;

        if (left + width > wa.Right)
            left = wa.Right - width;
        if (left < wa.Left)
            left = wa.Left;

        if (top + height > wa.Bottom)
            top = wa.Bottom - height;
        if (top < wa.Top) 
            top = wa.Top;

        window.Left = left;
        window.Top = top;
    }
}
