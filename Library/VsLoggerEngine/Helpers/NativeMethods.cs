using System.Runtime.InteropServices;
using System.Windows;

namespace VsLoggerEngine.Helpers;

public static class NativeMethods
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    public static void RestoreFocus(Window dialog)
    {
        if (dialog == null)
            return;

        const string previousActiveHandleFieldName = "_dialogPreviousActiveHandle";
        var baseType = dialog.GetType().BaseType;
        if (baseType == null) return;

        var field = baseType.GetField(previousActiveHandleFieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field == null) return;

        if (field.GetValue(dialog) is not IntPtr previousHandle || previousHandle == IntPtr.Zero)
            return;

        if (IsWindow(previousHandle))
        {
            SetForegroundWindow(previousHandle);
        }
    }
}
