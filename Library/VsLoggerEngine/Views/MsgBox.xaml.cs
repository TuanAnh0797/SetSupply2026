using VsLoggerEngine.Helpers;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace VsLoggerEngine.Views;

public class DialogOwnerWindow
{
    public static DialogOwnerWindow From(Window window) => new DialogOwnerWindow(window);
    public static implicit operator DialogOwnerWindow(Window window) => From(window);

    private readonly Window _window;

    public DialogOwnerWindow(Window window)
    {
        _window = window;
    }

    public void SetOwnerTo(Window dialog)
    {
        dialog.Owner = _window;
    }

    public IntPtr GetOwnerHandle() => _window is null ? IntPtr.Zero : new WindowInteropHelper(_window).Handle;
}

public partial class MsgBox : Window
{
    private static MsgBox? _currentDialog;
    private bool? _msgResult;

    private readonly SolidColorBrush InfoColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
    private readonly SolidColorBrush SuccessColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E8B57"));
    private readonly SolidColorBrush WarningColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA000"));
    private readonly SolidColorBrush ErrorColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
    private readonly SolidColorBrush QuestionColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0288D1"));

    public MsgBox(DialogOwnerWindow? owner, string message, string? title, MessageBoxType msgBoxType, MessageBoxButtons msgBoxButtons)
    {
        InitializeComponent();
        owner?.SetOwnerTo(this);

        Dispatcher.Invoke(() =>
        {
            tbTitle.Text = !string.IsNullOrEmpty(title) ? title : GetDefaultTitle(msgBoxType);
            tbMsg.Text = message;

            LoadMsgIcon(msgBoxType);
            LoadMsgButtons(msgBoxButtons);
        });
    }

    private string GetDefaultTitle(MessageBoxType msgBoxType)
    {
        var lang = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        return msgBoxType switch
        {
            MessageBoxType.Info => lang switch
            {
                "vi" => "Thông tin",
                "zh" => "信息",
                "ko" => "정보",
                _ => "Information"
            },
            MessageBoxType.Success => lang switch
            {
                "vi" => "Thành công",
                "zh" => "成功",
                "ko" => "성공",
                _ => "Successfully"
            },
            MessageBoxType.Warning => lang switch
            {
                "vi" => "Cảnh báo",
                "zh" => "警告",
                "ko" => "경고",
                _ => "Warning"
            },
            MessageBoxType.Error => lang switch
            {
                "vi" => "Lỗi",
                "zh" => "错误",
                "ko" => "오류",
                _ => "Error"
            },
            MessageBoxType.Question => lang switch
            {
                "vi" => "Câu hỏi",
                "zh" => "问题",
                "ko" => "질문",
                _ => "Question"
            },
            _ => string.Empty
        };
    }

    private void LoadMsgIcon(MessageBoxType msgBoxType)
    {
        switch (msgBoxType)
        {
            case MessageBoxType.Success:
                msgIcon.Kind = PackIconKind.CheckCircle;
                msgIcon.Foreground = SuccessColor;
                break;
            case MessageBoxType.Warning:
                msgIcon.Kind = PackIconKind.Alert;
                msgIcon.Foreground = WarningColor;
                break;
            case MessageBoxType.Error:
                msgIcon.Kind = PackIconKind.AlertCircle;
                msgIcon.Foreground = ErrorColor;
                break;
            case MessageBoxType.Question:
                msgIcon.Kind = PackIconKind.HelpCircle;
                msgIcon.Foreground = QuestionColor;
                break;
            default:
                msgIcon.Kind = PackIconKind.Information;
                msgIcon.Foreground = InfoColor;
                break;
        }
    }

    private void LoadMsgButtons(MessageBoxButtons msgBoxButtons)
    {
        string lang = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        string okText = lang switch
        {
            "vi" => "Đồng ý",
            "zh" => "确定",
            "ko" => "확인",
            _ => "OK"
        };

        string yesText = lang switch
        {
            "vi" => "Có",
            "zh" => "是",
            "ko" => "예",
            _ => "Yes"
        };

        string noText = lang switch
        {
            "vi" => "Không",
            "zh" => "否",
            "ko" => "아니요",
            _ => "No"
        };

        string cancelText = lang switch
        {
            "vi" => "Hủy",
            "zh" => "取消",
            "ko" => "취소",
            _ => "Cancel"
        };

        switch (msgBoxButtons)
        {
            case MessageBoxButtons.Ok:
                btnClose.Visibility = Visibility.Collapsed;
                btnOk.Content = okText;
                btnCloseMessage.Visibility = Visibility.Collapsed;
                break;

            case MessageBoxButtons.YesNo:
                btnClose.Content = noText;
                btnOk.Content = yesText;
                btnCloseMessage.Visibility = Visibility.Collapsed;
                break;

            case MessageBoxButtons.YesNoCancel:
                btnClose.Content = noText;
                btnOk.Content = yesText;
                btnCloseMessage.Content = cancelText;
                break;

            default:
                btnClose.Visibility = Visibility.Collapsed;
                btnOk.Content = okText;
                btnCloseMessage.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        this._msgResult = false;
        Close();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        this._msgResult = true;
        Close();
    }

    private void BtnCloseMessage_Click(object sender, RoutedEventArgs e)
    {
        this._msgResult = null;
        Close();
    }

    public static bool? Show(DialogOwnerWindow owner,
        string msg, string? title = null,
        MessageBoxType msgBoxType = MessageBoxType.Info,
        MessageBoxButtons msgBoxButtons = MessageBoxButtons.Ok,
        MessageInsertBehavior messageInsertBehavior = MessageInsertBehavior.Override)
    {
        if (_currentDialog != null)
        {
            if (_currentDialog.IsVisible)
            {
                _currentDialog.Activate();
            }

            switch (messageInsertBehavior)
            {
                case MessageInsertBehavior.Override:
                    _currentDialog.tbMsg.Text = msg;
                    break;
                case MessageInsertBehavior.AppendInline:
                    if (!_currentDialog.tbMsg.Text.EndsWith(msg))
                    {
                        _currentDialog.tbMsg.Text += " " + msg;
                    }
                    break;
                case MessageInsertBehavior.AppendNewline:
                    if (!_currentDialog.tbMsg.Text.EndsWith(msg))
                    {
                        _currentDialog.tbMsg.Text += Environment.NewLine + msg;
                    }
                    break;
            }

            return _currentDialog._msgResult;
        }

        _currentDialog = new MsgBox(owner, msg, title, msgBoxType, msgBoxButtons);
        _currentDialog.Closed += (_, _) => _currentDialog = null;
        return _currentDialog.ShowMsg();
    }

    public static bool? ShowDialog(DialogOwnerWindow owner, string msg, string? title = null, MessageBoxType msgBoxType = MessageBoxType.Info, MessageBoxButtons msgBoxButtons = MessageBoxButtons.Ok)
    {
        var dialog = new MsgBox(owner, msg, title, msgBoxType, msgBoxButtons);
        return dialog.ShowMsgDialog();
    }

    private bool? ShowMsg()
    {
        FixPreviousActiveHandle(Owner, _currentDialog);
        this.Show();
        NativeMethods.RestoreFocus(_currentDialog);
        return this._msgResult;
    }

    private bool? ShowMsgDialog()
    {
        FixPreviousActiveHandle(Owner, this);
        this.ShowDialog();
        NativeMethods.RestoreFocus(this);
        return this._msgResult;
    }

    private static void FixPreviousActiveHandle(DialogOwnerWindow owner, MsgBox dialog)
    {
        const string previousActiveHandle = "_dialogPreviousActiveHandle";

        System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action<(DialogOwnerWindow owner, MsgBox dialog)>(param =>
        {
            var dialogPreviousActiveHandle = param.dialog.GetType().BaseType?.GetField(previousActiveHandle, BindingFlags.Instance | BindingFlags.NonPublic);

            if (dialogPreviousActiveHandle?.GetValue(param.dialog) is not IntPtr ptr || ptr != IntPtr.Zero) return;
            dialogPreviousActiveHandle.SetValue(param.dialog, param.owner.GetOwnerHandle());
        }), (owner, dialog));
    }
}

public enum MessageBoxType
{
    Info,
    Success,
    Warning,
    Error,
    Question,
}

public enum MessageBoxButtons
{
    Ok,
    YesNo,
    YesNoCancel,
    RegisterReturn
}

public enum MessageInsertBehavior
{
    Override,
    AppendInline,
    AppendNewline
}
