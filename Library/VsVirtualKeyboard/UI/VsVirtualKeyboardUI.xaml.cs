using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using VsVirtualKeyboard.Behavior;
using VsVirtualKeyboard.Const;
using VsVirtualKeyboard.Control;

namespace VsVirtualKeyboard.UI;

public partial class VsVirtualKeyboardUI : VirtualKeyboard
{
    private readonly TextBoxHandler _textBoxHandler;

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public decimal Minimum
    {
        get => (decimal)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public decimal Maximum
    {
        get => (decimal)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(VsVirtualKeyboardUI),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(decimal), typeof(VsVirtualKeyboardUI),
        new PropertyMetadata(decimal.MinValue));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(decimal), typeof(VsVirtualKeyboardUI),
        new PropertyMetadata(decimal.MaxValue));

    public VsVirtualKeyboardUI()
    {
        InitializeComponent();
        _textBoxHandler = new TextBoxHandler(VkbTextBox);

        OnKeyboardLanguageChanged += OnKeyboardLanguageChangedHandler;
        VkbTextBox.Loaded += VkbInput_Loaded;
        VkbTextBox.IsVisibleChanged += VkbInput_IsVisibleChanged;
        VkbPasswordBox.Loaded += VkbInput_Loaded;
        VkbPasswordBox.IsVisibleChanged += VkbInput_IsVisibleChanged;
        SetInputMethod();
    }

    private void VkbInput_Loaded(object sender, RoutedEventArgs e)
    {
        FocusVkbTextBox();
        FocusVkbPasswordBox();

        if (Layout != eVkLayout.Text && (string.IsNullOrEmpty(VkbTextBox.Text) || _textBoxHandler.IsOuterRange()))
        {
            _textBoxHandler.ClampNumericValue(true);
        }
    }

    private void VkbInput_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        FocusVkbTextBox();
        FocusVkbPasswordBox();

        if (Layout != eVkLayout.Text && (string.IsNullOrEmpty(VkbTextBox.Text) || _textBoxHandler.IsOuterRange()))
        {
            _textBoxHandler.ClampNumericValue(true);
        }
    }

    public void FocusVkbTextBox()
    {
        if (VkbTextBox.IsLoaded && VkbTextBox.IsVisible)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                VkbTextBox.Focus();
                Keyboard.Focus(VkbTextBox);
                VkbTextBox.CaretIndex = Math.Max(0, VkbTextBox.Text.Length);
            });
        }
    }

    public void FocusVkbPasswordBox()
    {
        if (VkbPasswordBox.IsLoaded && VkbPasswordBox.IsVisible)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                VkbPasswordBox.Focus();
                Keyboard.Focus(VkbPasswordBox);
            });
        }
    }

    private void OnKeyboardLanguageChangedHandler(object? sender, eLanguageCode e)
    {
        SetInputMethod();
    }

    private void SetInputMethod()
    {
        switch (CurrentLang)
        {
            case eLanguageCode.en_US:
                InputMethod.SetIsInputMethodEnabled(VkbTextBox, false);
                InputMethod.SetIsInputMethodSuspended(VkbTextBox, true);
                break;
            case eLanguageCode.vi_VN:
                InputMethod.SetIsInputMethodEnabled(VkbTextBox, false);
                InputMethod.SetIsInputMethodSuspended(VkbTextBox, true);
                InputMethod.SetPreferredImeState(VkbTextBox, InputMethodState.On);
                InputMethod.SetPreferredImeConversionMode(VkbTextBox, ImeConversionModeValues.Alphanumeric);
                break;
            case eLanguageCode.ko_KR:
            case eLanguageCode.zh_CN:
                InputMethod.SetIsInputMethodEnabled(VkbTextBox, true);
                InputMethod.SetIsInputMethodSuspended(VkbTextBox, false);
                InputMethod.SetPreferredImeState(VkbTextBox, InputMethodState.On);
                InputMethod.SetPreferredImeConversionMode(VkbTextBox, ImeConversionModeValues.Native);
                break;
        }
    }

    public void UpdateNumericLabelVisibility()
    {
        var showMinMax = Layout != eVkLayout.Text && (Minimum != decimal.MinValue || Maximum != decimal.MaxValue);
        if (Layout == eVkLayout.Numeric)
        {
            MinTbl.Visibility = showMinMax && Minimum != int.MinValue ? Visibility.Visible : Visibility.Collapsed;
            MaxTbl.Visibility = showMinMax && Maximum != int.MaxValue ? Visibility.Visible : Visibility.Collapsed;
        }
        else if (Layout == eVkLayout.Decimal)
        {
            MinTbl.Visibility = showMinMax && Minimum != decimal.MinValue ? Visibility.Visible : Visibility.Collapsed;
            MaxTbl.Visibility = showMinMax && Maximum != decimal.MaxValue ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            MinTbl.Visibility = Visibility.Collapsed;
            MaxTbl.Visibility = Visibility.Collapsed;
        }
    }

    public void RemoveNumericHandler()
    {
        _textBoxHandler.RemoveNumericHandler();
    }

    public void AddOrRemoveNumericHandler()
    {
        _textBoxHandler.AddOrRemoveNumericHandler(Layout);
    }

    public void SetMinimum(decimal min)
    {
        Minimum = min;
        _textBoxHandler.Minimum = min;
    }

    public void SetMaximum(decimal max)
    {
        Maximum = max;
        _textBoxHandler.Maximum = max;
    }

    public void SetDecimalFormat(string decimalFormat)
    {
        _textBoxHandler.DecimalFormat = decimalFormat;
    }

    public bool UpdateSourceBinding()
    {
        if (Layout != eVkLayout.Text && (string.IsNullOrEmpty(VkbTextBox.Text) || _textBoxHandler.IsOuterRange()))
        {
            _textBoxHandler.ClampNumericValue(false);
            return false;
        }

        if(Layout != eVkLayout.Text)
        {
            _textBoxHandler.ClampNumericValue(true);
        }
        else {
            VkbTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        return true;
    }
}
