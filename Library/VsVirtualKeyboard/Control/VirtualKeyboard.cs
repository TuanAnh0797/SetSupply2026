using SharpHook;
using SharpHook.Data;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using VsVirtualKeyboard.Const;
using VsVirtualKeyboard.Helper;

namespace VsVirtualKeyboard.Control;

public class VirtualKeyboard : UserControl, IDisposable
{
    #region Event Handler

    public EventHandler<eLanguageCode>? OnKeyboardLanguageChanged { get; set; }

    #endregion

    #region Variable

    private readonly EventSimulator _eventSimulator = new EventSimulator();
    private readonly TaskPoolGlobalHook _hook = new();
    protected Panel? _layoutRoot;
    protected IEnumerable<Key>? _keys;
    protected Button? _langBtn;
    protected Button? _inputModeBtn;

    private CancellationTokenSource? _repeatBackspaceCts;

    #endregion

    #region Event

    public static readonly RoutedEvent VirtualKeyDownEvent = EventManager.
    RegisterRoutedEvent(nameof(VirtualKeyDown), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VirtualKeyboard));

    public event RoutedEventHandler VirtualKeyDown
    {
        add { AddHandler(VirtualKeyDownEvent, value); }
        remove { RemoveHandler(VirtualKeyDownEvent, value); }
    }

    #endregion

    #region Dependency Property

    #region Layout

    public static readonly DependencyProperty LayoutProperty =
            DependencyProperty.Register("Layout", typeof(eVkLayout), typeof(VirtualKeyboard), new PropertyMetadata(eVkLayout.Text, OnLayoutChanged));

    public eVkLayout Layout
    {
        get { return (eVkLayout)GetValue(LayoutProperty); }
        set { SetValue(LayoutProperty, value); }
    }

    private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VirtualKeyboard virtualKeyboard)
        {
            var layout = (eVkLayout)e.NewValue;
            virtualKeyboard._keys = null;
            virtualKeyboard._langBtn = null;
            virtualKeyboard._inputModeBtn = null;
            virtualKeyboard.Dispatcher.BeginInvoke(() =>
            {
                virtualKeyboard.RefreshKeyboardLayout();
            }, DispatcherPriority.Loaded);
        }
    }

    #endregion

    #endregion

    #region Property

    public eLanguageCode CurrentLang { get; private set; }
    public bool IsPressedShift { get; private set; }
    public bool IsPressedCapsLock { get; private set; }
    public bool IsPressedCtrl { get; set; }
    public bool ImeMode { get; private set; }

    #endregion

    #region Constructor

    public VirtualKeyboard()
    {
        Loaded += KeyboardUserControl_Loaded;
        IsVisibleChanged += KeyboardUserControl_IsVisibleChanged;
        InputLanguageManager.Current.InputLanguageChanged += OnInputLanguageChanged;

        _ = RegisterHookEvents();
    }

    #endregion

    #region Private Method

    private void FindElements()
    {
        if (_layoutRoot is null && FindName("KeyboardLayoutRoot") is Panel layoutRoot)
        {
            _layoutRoot = layoutRoot;
        }

        if (_layoutRoot != null && _langBtn is null && _layoutRoot.FindFirstVisualChild<Button>(SpecialButtonName.LangBtn) is { } langBtn)
        {
            _langBtn = langBtn;
        }

        if (_layoutRoot != null && _inputModeBtn is null && _layoutRoot.FindFirstVisualChild<Button>(SpecialButtonName.InputModeBtn) is { } inputModeBtn)
        {
            _inputModeBtn = inputModeBtn;
        }

        if (_layoutRoot != null && (_keys is null || !_keys.Any()))
        {
            _keys = _layoutRoot.FindVisualChildren<Key>();
        }
    }

    private async Task RegisterHookEvents()
    {
        _hook.KeyPressed += Hook_KeyPressed;
        _hook.KeyReleased += Hook_KeyReleased;
        _hook.MouseReleased += Hook_MouseReleased;
        await _hook.RunAsync();
    }

    private void UnregisterHookEvents()
    {
        _hook.KeyPressed -= Hook_KeyPressed;
        _hook.KeyReleased -= Hook_KeyReleased;
        _hook.MouseReleased -= Hook_MouseReleased;
        _hook.Stop();
    }

    protected void UpdateKeys()
    {
        UpdateLangKey();

        ImeMode = ImeHelper.GetIMEMode();

        if (_keys != null && _keys.Any())
        {
            foreach (var keyButton in _keys)
            {
                keyButton.UpdateKey(Layout == eVkLayout.Text ? IsPressedShift : false, IsPressedCapsLock, CurrentLang, ImeMode);
            }
        }
    }

    protected void UpdateLangKey()
    {
        if (_langBtn != null)
        {
            _langBtn.Content = CurrentLang.ToString().Split("_")[1].ToUpper();
        }
    }

    protected bool UpdateInputModeKey()
    {
        var imeModeChanged = false;

        var imeMode = ImeHelper.GetIMEMode();
        if (ImeMode != imeMode)
        {
            ImeMode = imeMode;
            imeModeChanged = true;
        }

        if (_inputModeBtn != null)
        {
            switch (CurrentLang)
            {
                case eLanguageCode.ko_KR:
                    _inputModeBtn.Content = ImeMode ? "가" : "abc";
                    break;
                case eLanguageCode.zh_CN:
                    _inputModeBtn.Content = ImeMode ? "中" : "英";
                    break;
                default:
                    _inputModeBtn.Content = "abc";
                    break;
            }
        }

        return imeModeChanged;
    }

    protected void RefreshKeyboardLayout(string lang = "")
    {
        SetLanguage(lang);
        FindElements();
        UpdateInputModeKey();
        UpdateKeys();
    }

    private void ReleaseBackspaceRepeat()
    {
        if (_repeatBackspaceCts != null)
        {
            _repeatBackspaceCts.Cancel();
            _repeatBackspaceCts.Dispose();
            _repeatBackspaceCts = null;
            _eventSimulator.SimulateKeyRelease(KeyCode.VcBackspace);
        }
    }

    private void SetLanguage(string lang)
    {
        if (string.IsNullOrEmpty(lang))
        {
            lang = InputLanguageManager.Current.CurrentInputLanguage.Name;
        }

        CurrentLang = lang switch
        {
            "ko-KR" => eLanguageCode.ko_KR,
            "vi-VN" => eLanguageCode.vi_VN,
            "zh-CN" => eLanguageCode.zh_CN,
            _ => eLanguageCode.en_US
        };

        OnKeyboardLanguageChanged?.Invoke(this, CurrentLang);
    }


    private bool IsNumLockOn()
    {
        return Convert.ToBoolean(Win32Api.GetKeyState((int)KeyCode.VcNumLock) & 0x0001);
    }

    #endregion

    #region Event Handler

    private void OnInputLanguageChanged(object sender, InputLanguageEventArgs e)
    {
        RefreshKeyboardLayout(e.NewLanguage.Name);
    }

    private void KeyboardUserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }

        RemoveHandler(Key.ClickEvent, (RoutedEventHandler)KeyClick);
        AddHandler(Key.ClickEvent, (RoutedEventHandler)KeyClick);

        Dispatcher.BeginInvoke(() =>
        {
            RefreshKeyboardLayout();
        }, DispatcherPriority.SystemIdle);
    }

    private void KeyboardUserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not bool isVisible)
        {
            return;
        }

        if (!isVisible)
        {
            if (IsPressedShift)
            {
                IsPressedShift = false;
                _eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);
            }

            if (IsPressedCapsLock)
            {
                IsPressedCapsLock = false;
                _eventSimulator.SimulateKeyRelease(KeyCode.VcCapsLock);
            }
        }
    }

    private void Hook_KeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        var keyCode = e.Data.KeyCode;
        Dispatcher.BeginInvoke(() =>
        {
            var refreshLayout = false;
            if (UpdateInputModeKey())
            {
                refreshLayout = true;
            }

            switch (keyCode)
            {
                case KeyCode.VcLeftShift:
                case KeyCode.VcRightShift:
                    if (Layout == eVkLayout.Text)
                    {
                        IsPressedShift = true;
                    }
                    if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcLeftShift) is { } shiftKey)
                    {
                        shiftKey.IsPressed = true;
                    }
                    refreshLayout = true;
                    break;
                case KeyCode.VcCapsLock:
                    IsPressedCapsLock = !IsPressedCapsLock;
                    if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcCapsLock) is { } capsLockKey)
                    {
                        capsLockKey.IsPressed = true;
                    }
                    refreshLayout = true;
                    break;
                case KeyCode.VcLeftControl:
                case KeyCode.VcRightControl:
                    if (Layout == eVkLayout.Text)
                    {
                        IsPressedCtrl = true;
                    }
                    if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcLeftControl) is { } ctrlKey)
                    {
                        ctrlKey.IsPressed = true;
                    }
                    break;
                default:
                    if (_keys?.FirstOrDefault(k => k.KeyCode == keyCode) is { } keyBtn)
                    {
                        keyBtn.IsPressed = true;
                    }
                    break;
            }

            if (refreshLayout)
            {
                UpdateKeys();
            }
        }, DispatcherPriority.ApplicationIdle);
    }

    private void Hook_KeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        var keyCode = e.Data.KeyCode;
        Dispatcher.BeginInvoke(() =>
        {
            switch (keyCode)
            {
                case KeyCode.VcLeftShift:
                case KeyCode.VcRightShift:
                    IsPressedShift = false;
                    if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcLeftShift) is { } shiftKey)
                    {
                        shiftKey.IsPressed = false;
                    }
                    UpdateKeys();
                    break;
                case KeyCode.VcCapsLock:
                    if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcCapsLock) is { } capsLockKey)
                    {
                        capsLockKey.IsPressed = IsPressedCapsLock;
                    }
                    UpdateKeys();
                    break;
                case KeyCode.VcLeftControl:
                case KeyCode.VcRightControl:
                    IsPressedCtrl = false;
                    if (_keys?.FirstOrDefault(k => k.KeyCode == KeyCode.VcLeftControl) is { } ctrlKey)
                    {
                        ctrlKey.IsPressed = false;
                    }
                    break;
                default:
                    if (_keys?.FirstOrDefault(k => k.KeyCode == keyCode) is { } keyBtn)
                    {
                        keyBtn.IsPressed = false;
                    }
                    break;
            }
        }, DispatcherPriority.ApplicationIdle);
    }

    private void Hook_MouseReleased(object? sender, MouseHookEventArgs e)
    {
        ReleaseBackspaceRepeat();
    }

    private void KeyClick(object sender, RoutedEventArgs e)
    {
        if (Layout != eVkLayout.Text)
        {
            _eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);
            _eventSimulator.SimulateKeyRelease(KeyCode.VcRightShift);

            if (!IsNumLockOn())
            {
                _eventSimulator.SimulateKeyPress(KeyCode.VcNumLock);
                _eventSimulator.SimulateKeyRelease(KeyCode.VcNumLock);
            }
        }

        if (e.OriginalSource is Button { Name: SpecialButtonName.LangBtn } langBtn)
        {
            _eventSimulator.SimulateKeyPress(KeyCode.VcLeftAlt);
            _eventSimulator.SimulateKeyPress(KeyCode.VcLeftShift);
            _eventSimulator.SimulateKeyRelease(KeyCode.VcLeftAlt);
            _eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);
            return;
        }

        if (e.OriginalSource is Button { Name: SpecialButtonName.InputModeBtn } inputModeBtn)
        {
            switch (CurrentLang)
            {
                case eLanguageCode.ko_KR:
                    _eventSimulator.SimulateKeyPress(KeyCode.VcHangul);
                    _eventSimulator.SimulateKeyRelease(KeyCode.VcHangul);
                    break;
                case eLanguageCode.zh_CN:
                    _eventSimulator.SimulateKeyPress(KeyCode.VcLeftShift);
                    _eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);
                    break;
                default:
                    break;
            }
            return;
        }

        if (e.OriginalSource is not Key key)
        {
            return;
        }

        var keyCode = key.KeyCode;

        switch (keyCode)
        {
            case KeyCode.VcLeftShift:
                IsPressedShift = !IsPressedShift;
                key.IsPressed = IsPressedShift;
                if (IsPressedShift)
                {
                    _eventSimulator.SimulateKeyPress(keyCode);
                }
                else
                {
                    _eventSimulator.SimulateKeyRelease(keyCode);
                }
                break;
            case KeyCode.VcLeftControl:
                IsPressedCtrl = !IsPressedCtrl;
                key.IsPressed = IsPressedCtrl;
                if (IsPressedCtrl)
                {
                    _eventSimulator.SimulateKeyPress(keyCode);
                }
                else
                {
                    _eventSimulator.SimulateKeyRelease(keyCode);
                }
                break;
            case KeyCode.VcBackspace:
                if (_repeatBackspaceCts != null)
                    return;

                _eventSimulator.SimulateKeyPress(keyCode);
                _repeatBackspaceCts = new CancellationTokenSource();
                var token = _repeatBackspaceCts.Token;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(400);

                    while (!token.IsCancellationRequested)
                    {
                        _eventSimulator.SimulateKeyPress(keyCode);
                        await Task.Delay(50);
                    }
                }, token);
                break;
            case KeyCode.VcEnter:
                _eventSimulator.SimulateKeyPress(keyCode);
                _eventSimulator.SimulateKeyRelease(keyCode);
                break;
            case KeyCode.VcTab:
                _eventSimulator.SimulateTextEntry("    ");
                break;
            default:
                _eventSimulator.SimulateKeyPress(keyCode);
                _eventSimulator.SimulateKeyRelease(keyCode);

                if (IsPressedCtrl)
                {
                    _eventSimulator.SimulateKeyRelease(KeyCode.VcLeftControl);
                }

                if (IsPressedShift)
                {
                    _eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);
                }
                break;
        }

        if (keyCode is KeyCode.VcLeftShift or KeyCode.VcRightShift or KeyCode.VcCapsLock)
        {
            Dispatcher.Invoke(UpdateKeys);
        }

        RaiseEvent(new RoutedEventArgs(VirtualKeyDownEvent, key.KeyCode));
    }

    #endregion


    #region Disposable

    public void Dispose()
    {
        ReleasePressedKeys();
        UnregisterHookEvents();
    }

    public void ReleasePressedKeys()
    {
        ReleaseBackspaceRepeat();

        if (IsPressedCapsLock)
        {
            _eventSimulator.SimulateKeyRelease(KeyCode.VcCapsLock);
        }

        if (IsPressedShift)
        {
            _eventSimulator.SimulateKeyRelease(KeyCode.VcLeftShift);
            _eventSimulator.SimulateKeyRelease(KeyCode.VcRightShift);
        }

        if (IsPressedCtrl)
        {
            _eventSimulator.SimulateKeyRelease(KeyCode.VcLeftControl);
            _eventSimulator.SimulateKeyRelease(KeyCode.VcRightControl);
        }
    }

    #endregion
}
