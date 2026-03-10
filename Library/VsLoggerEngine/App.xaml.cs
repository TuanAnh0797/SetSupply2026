using System.Windows;
using System.Windows.Navigation;

namespace VsLoggerEngine;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private Mutex? _appMutex;
    private NotifyIcon? _notifyIcon;
    public static string CallerAppName { get; private set; } = "Unknow";
    public static string LogFolderPath { get; private set; } = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var callerArg = e.Args.FirstOrDefault(arg => arg.StartsWith(Application.VsLoggerEngine.CALLER_ARG))?.Split("=");
        if (callerArg != null && callerArg.Length == 2)
        {
            CallerAppName = callerArg[1];
        }

        var logFolderPathArg = e.Args.FirstOrDefault(a => a.StartsWith(Application.VsLoggerEngine.LOG_FOLDER_PATH_ARG))?.Split("=");
        if (logFolderPathArg != null && logFolderPathArg.Length == 2)
        {
            LogFolderPath = logFolderPathArg[1];
        }

        _appMutex = new Mutex(true, $"{Application.VsLoggerEngine.APP_NAME}_{CallerAppName}", out var createdNew);
        if (!createdNew)
        {
            _appMutex.Dispose();
            Current.Shutdown();
            return;
        }

        CreateTrayIcon();

        if (CallerAppName != "Unknow")
        {
            Application.VsLoggerEngine.NotifyReady(CallerAppName);
        }

        Current.MainWindow = new LoggerMainWindow();

        _notifyIcon?.ShowBalloonTip(1000, "VS Logger", $"VS Logger - {CallerAppName} is running", ToolTipIcon.None);
    }

    protected override void OnLoadCompleted(NavigationEventArgs e)
    {
        base.OnLoadCompleted(e);
    }

    private void CreateTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = new Icon("Resources/Icons/vslogger_icon.ico"),
            Visible = true,
            Text = $"VS Logger - {CallerAppName}"
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open", null, (s, ev) =>
        {
            if (Current.MainWindow is { } mainWindow)
            {
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Show();
            }
        });

        contextMenu.Items.Add("Options", null, (s, ev) =>
        {
            if (Current.MainWindow is LoggerMainWindow mainWindow)
            {
                mainWindow.ViewModel.OnOpenLoggerOptions(null);
            }
        });

        contextMenu.Items.Add(new ToolStripSeparator());

        contextMenu.Items.Add("Quit", null, (s, ev) =>
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Current.Shutdown();
        });

        _notifyIcon.ContextMenuStrip = contextMenu;

        _notifyIcon.DoubleClick += (s, ev) =>
        {
            if (Current.MainWindow is { } mainWindow)
            {
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Show();
            }
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        _notifyIcon?.Dispose();
        _appMutex?.ReleaseMutex();
        _appMutex?.Dispose();
    }
}
