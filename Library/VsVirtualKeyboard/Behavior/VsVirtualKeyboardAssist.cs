using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using VsVirtualKeyboard.Const;
using VsVirtualKeyboard.Control;
using VsVirtualKeyboard.Helper;
using VsVirtualKeyboard.Provider;
using VsVirtualKeyboard.UI;

namespace VsVirtualKeyboard.Behavior;

public static class VsVirtualKeyboardAssist
{
    #region Variable

    private static VirtualKeyboardWindow? _virtualKeyboardWindow = null;
    private static DependencyObject? _placementTarget = null;

    #endregion

    #region Global Property

    public static bool EnableVirtualKeyboard { get; set; } = true;

    #endregion

    #region Func/Action

    public static Func<DependencyObject, eVkLayout>? GetKeyboardLayoutAction = null;
    public static Action<DependencyObject, eVkLayout, VirtualKeyboardWindow>? SetBindingAction = null;

    #endregion

    #region Dependency Property

    #region Layout Property

    public static readonly DependencyProperty LayoutProperty =
        DependencyProperty.RegisterAttached(
            "Layout",
            typeof(eVkLayout),
            typeof(VsVirtualKeyboardAssist),
            new PropertyMetadata(default(eVkLayout), OnLayoutChanged));

    private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            UpdateOrSetTextBoxHandler(textBox);
        }
    }

    public static eVkLayout GetLayout(DependencyObject obj) =>
        (eVkLayout)obj.GetValue(LayoutProperty);

    public static void SetLayout(DependencyObject obj, eVkLayout value) =>
        obj.SetValue(LayoutProperty, value);

    #endregion

    #region UseVirtualKeyBoard Property

    public static readonly DependencyProperty UseVirtualKeyBoardProperty =
    DependencyProperty.RegisterAttached(
        "UseVirtualKeyBoard",
        typeof(bool),
        typeof(VsVirtualKeyboardAssist),
        new PropertyMetadata(false, OnUseVirtualKeyBoardChanged));

    public static void SetUseVirtualKeyBoard(DependencyObject obj, bool value) =>
        obj.SetValue(UseVirtualKeyBoardProperty, value);

    public static bool GetUseVirtualKeyBoard(DependencyObject obj) =>
        (bool)obj.GetValue(UseVirtualKeyBoardProperty);

    #endregion

    #region Minimum Property

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.RegisterAttached(
            "Minimum",
            typeof(decimal),
            typeof(VsVirtualKeyboardAssist),
            new PropertyMetadata(decimal.MinValue, OnMinimumChanged));

    private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            UpdateOrSetTextBoxHandler(textBox);
        }
    }

    public static void SetMinimum(DependencyObject obj, decimal value) =>
        obj.SetValue(MinimumProperty, value);

    public static decimal GetMinimum(DependencyObject obj) =>
        (decimal)obj.GetValue(MinimumProperty);

    #endregion

    #region Maximum Property

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.RegisterAttached(
            "Maximum",
            typeof(decimal),
            typeof(VsVirtualKeyboardAssist),
            new PropertyMetadata(decimal.MaxValue, OnMaximumChanged));

    private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            UpdateOrSetTextBoxHandler(textBox);
        }
    }

    public static void SetMaximum(DependencyObject obj, decimal value) =>
        obj.SetValue(MaximumProperty, value);

    public static decimal GetMaximum(DependencyObject obj) =>
        (decimal)obj.GetValue(MaximumProperty);

    #endregion

    #region DecimalFormat Property

    public static readonly DependencyProperty DecimalFormatProperty =
        DependencyProperty.RegisterAttached(
            "DecimalFormat",
            typeof(string),
            typeof(VsVirtualKeyboardAssist),
            new PropertyMetadata(string.Empty, OnDecimalFormatChanged));

    private static void OnDecimalFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            UpdateOrSetTextBoxHandler(textBox);
        }
    }

    public static void SetDecimalFormat(DependencyObject obj, string value) =>
        obj.SetValue(DecimalFormatProperty, value);

    public static string GetDecimalFormat(DependencyObject obj) =>
        (string)obj.GetValue(DecimalFormatProperty);

    #endregion

    #region TextBoxHandler Property

    public static readonly DependencyProperty TextBoxHandlerProperty =
        DependencyProperty.RegisterAttached(
            "TextBoxHandler",
            typeof(TextBoxHandler),
            typeof(VsVirtualKeyboardAssist),
            new PropertyMetadata(null));

    public static TextBoxHandler? GetTextBoxHandler(DependencyObject obj) =>
        (TextBoxHandler?)obj.GetValue(TextBoxHandlerProperty);

    public static void SetTextBoxHandler(DependencyObject obj, TextBoxHandler? value) =>
        obj.SetValue(TextBoxHandlerProperty, value);
    
    #endregion

    #region VkbIconAdorner Property

    private static readonly DependencyProperty VkbIconAdornerProperty =
       DependencyProperty.RegisterAttached(
           "VkbIconAdorner", typeof(VkbIconAdorner), typeof(VsVirtualKeyboardAssist),
           new PropertyMetadata(null));
    private static void SetVkbIconAdorner(UIElement element, VkbIconAdorner value)
        => element.SetValue(VkbIconAdornerProperty, value);
    private static VkbIconAdorner? GetVkbIconAdorner(UIElement element)
        => (VkbIconAdorner?)element.GetValue(VkbIconAdornerProperty);

    #endregion

    #region Action Provider Property

    public static readonly DependencyProperty EnterActionProviderProperty =
        DependencyProperty.RegisterAttached(
            "EnterActionProvider",
            typeof(IEnterActionProvider),
            typeof(VsVirtualKeyboardAssist),
            new PropertyMetadata(null, OnEnterActionProviderChanged));

    public static IEnterActionProvider? GetEnterActionProvider(DependencyObject obj)
        => (IEnterActionProvider?)obj.GetValue(EnterActionProviderProperty);

    public static void SetEnterActionProvider(DependencyObject obj, IEnterActionProvider value)
        => obj.SetValue(EnterActionProviderProperty, value);

    private static void OnEnterActionProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is IEnterActionProvider provider)
        {
            provider.PlacementTarget = d;
        }
    }
    #endregion

    #endregion

    #region Contructor

    static VsVirtualKeyboardAssist()
    {
        SetBindingAction = SetBinding;
        GetKeyboardLayoutAction = GetLayout;
    }

    #endregion


    #region Event Handlers

    private static void OnUseVirtualKeyBoardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(e.NewValue is bool enabled) || d is not UIElement ui) return;

        if (enabled)
        {
            ui.PreviewMouseDown += OnControlMouseDown;
            ui.IsEnabledChanged += OnIsEnabledChanged;
            ui.IsHitTestVisibleChanged += OnIsEnabledChanged;
            ui.IsVisibleChanged += OnIsVisibleChanged;
        }
        else
        {
            ui.PreviewMouseDown -= OnControlMouseDown;
            ui.IsEnabledChanged -= OnIsEnabledChanged;
            ui.IsHitTestVisibleChanged -= OnIsEnabledChanged;
            ui.IsVisibleChanged -= OnIsVisibleChanged;
        }
    }

    private static void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var isEnable = (bool)e.NewValue;

        if (!isEnable && _virtualKeyboardWindow?.IsVisible == true)
        {
            _virtualKeyboardWindow.Hide();
        }
    }

    private static void OnControlMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DependencyObject d)
        {
            ToggleVirtualKeyBoard(d);
        }
    }

    private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var isVisible = (bool)e.NewValue;
        if (!isVisible && _virtualKeyboardWindow?.IsVisible == true)
        {
            _virtualKeyboardWindow.Hide();
        }
    }

    private static void ToggleVirtualKeyBoard(DependencyObject placementTarget)
    {
        if (!EnableVirtualKeyboard)
        {
            return;
        }

        if (_virtualKeyboardWindow != null && _virtualKeyboardWindow.IsVisible && !TargetChanged(placementTarget) || IsReadOnly(placementTarget) == true)
        {
            _virtualKeyboardWindow?.Hide();
            return;
        }

        ShowVirtualKeyboard(placementTarget);
    }

    private static bool? IsReadOnly(DependencyObject placementTarget)
    {
        var prop = placementTarget.GetType().GetProperty("IsReadOnly");
        return (bool?)prop?.GetValue(placementTarget);
    }

    #endregion

    #region Public Methods

    public static bool TargetChanged(DependencyObject? placementTarget)
    {
        return _placementTarget != placementTarget;
    }

    public static void ShowVirtualKeyboard(DependencyObject? placementTarget)
    {
        if (placementTarget is null)
        {
            return;
        }

        SetVkbIconVisibility(placementTarget, true);

        if (_virtualKeyboardWindow == null)
        {
            _virtualKeyboardWindow = new();
        }

        _placementTarget = placementTarget;
        var layout = GetKeyboardLayoutAction?.Invoke(placementTarget) ?? default;

        _virtualKeyboardWindow.SetLayout(layout);
        _virtualKeyboardWindow.SetPlacementTarget(_placementTarget);

        SetBindingAction?.Invoke(placementTarget, layout, _virtualKeyboardWindow);

        _virtualKeyboardWindow.Show();
        _virtualKeyboardWindow.Activate();
    }

    public static void SetVkbIconVisibility(DependencyObject? placementTarget, bool visible)
    {
        if (placementTarget is not UIElement uIElement || AdornerLayer.GetAdornerLayer(uIElement) is not { } adornerLayer)
        {
            return;
        }

        if (visible)
        {
            var vkbIcon = new VkbIconAdorner(uIElement);
            vkbIcon.SetPreviewMouseDownAction((_, _) =>
            {
                _virtualKeyboardWindow?.Hide();
            });
            adornerLayer.Add(vkbIcon);
            SetVkbIconAdorner(uIElement, vkbIcon);
        }
        else
        {
            if (GetVkbIconAdorner(uIElement) is { } vkbIcon)
            {
                adornerLayer.Remove(vkbIcon);
            }
        }
    }

    public static void SetBinding(DependencyObject placementTarget, eVkLayout layout, VirtualKeyboardWindow virtualKeyboardWindow)
    {
        if (placementTarget is TextBox textBox)
        {
            var binding = new Binding("Text")
            {
                Source = textBox,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            var min = GetMinimum(placementTarget);
            var max = GetMaximum(placementTarget);

            switch (layout)
            {
                case eVkLayout.Numeric:
                    virtualKeyboardWindow.SetBinding(binding, Math.Max(int.MinValue, min), Math.Min(int.MaxValue, max));
                    break;
                case eVkLayout.Decimal:
                    var decimalFormat = GetDecimalFormat(placementTarget);
                    virtualKeyboardWindow.SetBinding(binding, min, max, decimalFormat);
                    break;
                default:
                    virtualKeyboardWindow.SetBinding(binding);
                    break;
            }
        }
        else if (placementTarget is PasswordBox passwordBox)
        {
            virtualKeyboardWindow.SetBinding(null);
        }
    }

    public static void ResetVirtualKeyboard()
    {
        _virtualKeyboardWindow = null;
        _placementTarget = null;
    }

    public static List<IEnterActionProvider> GetEnterActionProviders(DependencyObject d)
    {
        var list = new List<IEnterActionProvider>();

        if (GetEnterActionProvider(d) is { } provider)
        {
            list.Add(provider);
        }

        if (TreeHelper.FindParent<VkEnterActionGroupProvider>(d) is { } groupControl)
        {
            var subProviders = GetProviders(groupControl, []) ?? [];
            list.AddRange(subProviders.Where(x => x.PlacementTarget != d));

            if (groupControl.Commit is { } commitProvider)
            {
                if (subProviders.Any())
                {
                    commitProvider.PlacementTarget = subProviders.Last().PlacementTarget;
                }

                list.Add(commitProvider);
            }
        }

        return list;
    }

    public static IEnumerable<IEnterActionProvider> GetProviders(DependencyObject root, DependencyObject[] excepts)
    {
        if (root == null) yield break;

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (!excepts.Contains(child) && GetEnterActionProvider(child) is { } provider)
            {
                yield return provider;
            }

            foreach (var descendant in GetProviders(child, excepts))
                yield return descendant;
        }
    }

    public static void UpdateOrSetTextBoxHandler(TextBox textBox)
    {
        var handler = GetTextBoxHandler(textBox);
        if (handler == null)
        {
            handler = new TextBoxHandler(textBox);
            handler.RegisterLostFocusAndEnterEvents();
            SetTextBoxHandler(textBox, handler);
        }
        handler.Layout = GetLayout(textBox);
        handler.Minimum = GetMinimum(textBox);
        handler.Maximum = GetMaximum(textBox);
        handler.DecimalFormat = GetDecimalFormat(textBox);
        handler.AddOrRemoveNumericHandler(handler.Layout);
        handler.ClampNumericValue(true);
    }

    #endregion
}
