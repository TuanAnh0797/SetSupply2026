using System.Windows;
using System.Windows.Controls;
using VsVirtualKeyboard.Behavior;
using VsVirtualKeyboard.UI;

namespace VsVirtualKeyboard;

public class KeyBoardSetup
{
    public static void Initialize()
    {
        SetupVirtualKeyboard();
    }

    private static void SetupVirtualKeyboard()
    {
        EventManager.RegisterClassHandler(
            typeof(TextBox),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnInputControlLoaded));

        EventManager.RegisterClassHandler(
            typeof(PasswordBox),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnInputControlLoaded));
    }

    private static void OnInputControlLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            if (Window.GetWindow(tb) is VirtualKeyboardWindow)
            {
                return;
            }

            if (!VsVirtualKeyboardAssist.GetUseVirtualKeyBoard(tb))
                VsVirtualKeyboardAssist.SetUseVirtualKeyBoard(tb, true);
        }
        else if (sender is PasswordBox pb)
        {
            if (Window.GetWindow(pb) is VirtualKeyboardWindow)
            {
                return;
            }

            if (!VsVirtualKeyboardAssist.GetUseVirtualKeyBoard(pb))
                VsVirtualKeyboardAssist.SetUseVirtualKeyBoard(pb, true);
        }
    }
}
