using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using VsVirtualKeyboard.Behavior;
using VsVirtualKeyboard.Const;
using VsVirtualKeyboard.Helper;

namespace VsVirtualKeyboard.UI;

/// <summary>
/// Interaction logic for VirtualKeyboardWindow.xaml
/// </summary>
public partial class VirtualKeyboardWindow : Window
{
    private DependencyObject? _placementTarget;

    public VirtualKeyboardWindow()
    {
        InitializeComponent();
        SizeChanged += VirtualKeyboardWindow_SizeChanged;
        LocationChanged += VirtualKeyboardWindow_LocationChanged;
        IsVisibleChanged += VirtualKeyboardWindow_IsVisibleChanged;
        MouseDown += VirtualKeyboardWindow_MouseDown;
        PreviewKeyUp += VirtualKeyboardWindow_KeyUp;
        Closing += VirtualKeyboardWindow_Closing;
        Application.Current.Exit += Application_Exit;
    }

    private void VirtualKeyboardWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        CalcKeyboardPosition();
    }

    private void VirtualKeyboardWindow_LocationChanged(object? sender, EventArgs e)
    {
        VirtualKeyboardComp.FocusVkbTextBox();
    }

    private void VirtualKeyboardWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var isVisible = (bool)e.NewValue;
        if (!isVisible)
        {
            VsVirtualKeyboardAssist.SetVkbIconVisibility(_placementTarget, false);
            VirtualKeyboardComp.ReleasePressedKeys();
            VirtualKeyboardComp.RemoveNumericHandler();
            BindingOperations.ClearBinding(VirtualKeyboardComp, VsVirtualKeyboardUI.ValueProperty);
            ClearText();
            SetPlacementTarget(null);
        }
        else
        {
            if (IsLoaded)
            {
                CalcKeyboardPosition();
            }
        }
    }

    private void VirtualKeyboardWindow_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Mouse.LeftButton == MouseButtonState.Pressed)
            this.DragMove();
    }

    private async void VirtualKeyboardWindow_KeyUp(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                if (!UpdateSourceBinding())
                {
                    return;
                }

                var result = await InvokeProvders();
                if (!result)
                {
                    return;
                }

                FocusPlacementTarget();
                ClearText();
                await Task.Delay(100);
                Hide();
                break;
            case Key.Escape:
                FocusPlacementTarget();
                ClearText();
                await Task.Delay(100);
                Hide();
                break;
            default:
                break;
        }
    }

    private async Task<bool> InvokeProvders()
    {
        if (_placementTarget != null)
        {
            var providers = VsVirtualKeyboardAssist.GetEnterActionProviders(_placementTarget);
            foreach (var provider in providers)
            {
                var result = await provider.ExecuteAsync();
                result.Show(VirtualKeyboardComp.MsgTbl);
                if (!result.IsAccepted())
                {
                    if (provider.PlacementTarget != null && VsVirtualKeyboardAssist.TargetChanged(provider.PlacementTarget))
                    {
                        VsVirtualKeyboardAssist.ShowVirtualKeyboard(provider.PlacementTarget);
                    }

                    return false;
                }
            }
        }

        return true;
    }

    private void VirtualKeyboardWindow_Closing(object? sender, CancelEventArgs e)
    {
        VsVirtualKeyboardAssist.SetVkbIconVisibility(_placementTarget, false);
        VirtualKeyboardComp?.Dispose();
        VsVirtualKeyboardAssist.ResetVirtualKeyboard();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        VirtualKeyboardComp?.Dispose();
    }

    private void CalcKeyboardPosition()
    {
        if (_placementTarget is not UIElement ui)
            return;

        var pt = ui.PointToScreen(new Point(0, 0));
        var wa = SystemParameters.WorkArea;

        double windowHeight = ActualHeight;
        double windowWidth = ActualWidth;

        if (windowHeight <= 0 || windowWidth <= 0)
            return;

        double desiredTop;
        if (pt.Y + ui.RenderSize.Height + windowHeight > wa.Bottom)
        {
            desiredTop = pt.Y - windowHeight;
            if (desiredTop < wa.Top)
                desiredTop = wa.Top;
        }
        else
        {
            desiredTop = pt.Y + ui.RenderSize.Height;
        }

        double desiredLeft = Math.Min(Math.Max(pt.X, wa.Left), wa.Right - windowWidth);

        Left = desiredLeft;
        Top = desiredTop;

        ShiftWindowOntoScreenHelper.ShiftWindowOntoScreen(this);
    }

    public void SetLayout(eVkLayout layout)
    {
        VirtualKeyboardComp.Layout = layout;

        SetDisplayKeyboardSize(layout);

        SetDefaultMinMaxValue(layout);

        UpdateVisibilityState();
    }

    private void SetDisplayKeyboardSize(eVkLayout layout)
    {
        MaxWidth = layout == eVkLayout.Text ? 948 : 548;
    }

    private void SetDefaultMinMaxValue(eVkLayout layout)
    {
        if (layout == eVkLayout.Decimal)
        {
            VirtualKeyboardComp.SetMinimum(decimal.MinValue);
            VirtualKeyboardComp.SetMaximum(decimal.MaxValue);
        }
        else
        {
            VirtualKeyboardComp.SetMinimum(int.MinValue);
            VirtualKeyboardComp.SetMaximum(int.MaxValue);
        }
    }

    public void SetBinding(Binding binding, decimal min = decimal.MinValue, decimal max = decimal.MaxValue, string decimalFormat = "")
    {
        if (_placementTarget is PasswordBox passwordBox)
        {
            VirtualKeyboardComp.VkbPasswordBox.Password = passwordBox.Password;
        }
        else
        {
            BindingOperations.SetBinding(VirtualKeyboardComp, VsVirtualKeyboardUI.ValueProperty, binding);
            VirtualKeyboardComp.SetMinimum(min);
            VirtualKeyboardComp.SetMaximum(max);
            VirtualKeyboardComp.SetDecimalFormat(decimalFormat);
        }

        UpdateVisibilityState();
        VirtualKeyboardComp.AddOrRemoveNumericHandler();
    }

    public void SetPlacementTarget(DependencyObject? placementTarget)
    {
        VsVirtualKeyboardAssist.SetVkbIconVisibility(_placementTarget, false);

        _placementTarget = placementTarget;

        if (_placementTarget is PasswordBox passwordBox)
        {
            VirtualKeyboardComp.VkbPasswordBox.Visibility = Visibility.Visible;
            VirtualKeyboardComp.VkbTextBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            VirtualKeyboardComp.VkbPasswordBox.Visibility = Visibility.Collapsed;
            VirtualKeyboardComp.VkbTextBox.Visibility = Visibility.Visible;
        }

        if (IsLoaded)
        {
            CalcKeyboardPosition();
        }

        UpdateWindowOwner();
    }

    private void UpdateWindowOwner()
    {
        if (_placementTarget != null)
        {
            var ownerWindow = Window.GetWindow(_placementTarget);
            ownerWindow.Closing -= OwnerWindow_Closing;
            ownerWindow.Closing += OwnerWindow_Closing;
            SetWindowOwner(ownerWindow);
        }
        else
        {
            SetWindowOwner(null);
        }
    }

    private void OwnerWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (IsVisible)
        {
            SetWindowOwner(null);
            Hide();
        }
    }

    private void SetWindowOwner(Window? owner)
    {
        Owner = owner;
    }

    private bool UpdateSourceBinding()
    {
        if (_placementTarget is PasswordBox passwordBox)
        {
            passwordBox.Password = VirtualKeyboardComp.VkbPasswordBox.Password;
            return true;
        }
        else
        {
            return VirtualKeyboardComp.UpdateSourceBinding();
        }
    }

    private void UpdateVisibilityState()
    {
        VirtualKeyboardComp.UpdateNumericLabelVisibility();
    }

    private void ClearText()
    {
        VirtualKeyboardComp.VkbPasswordBox.Password = string.Empty;
        VirtualKeyboardComp.MsgTbl.Text = string.Empty;
    }

    private void FocusPlacementTarget()
    {
        if (_placementTarget?.FindFirstVisualChild<TextBox>() is { } textBox)
        {
            textBox.Focus();
            textBox.CaretIndex = textBox.Text.Length;
        }
        else if (_placementTarget is PasswordBox passwordBox)
        {
            passwordBox.Focus();
        }
    }
}
