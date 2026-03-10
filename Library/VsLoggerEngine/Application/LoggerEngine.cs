using VsLoggerEngine.Constants;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Management;
using System.Reflection;
using System.Text;

namespace VsLoggerEngine.Application;

public static class VsLoggerEngine
{
    public static readonly string APP_NAME = "VsLoggerEngine";
    public static readonly string CALLER_ARG = "--caller";
    public static readonly string LOG_FOLDER_PATH_ARG = "--logFolderPath";

    public static void Start(string callerAppName, string logFolderPath = "")
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var executeAssembly = Path.Combine(assemblyDirectory, $"{APP_NAME}.exe");
        var mutexName = $"{APP_NAME}_{callerAppName}";

        if (Mutex.TryOpenExisting(mutexName, out _))
        {
            return;
        }

        if (File.Exists(executeAssembly))
        {
            var argsBuilder = new StringBuilder();
            argsBuilder.Append($"{CALLER_ARG}=\"{callerAppName}\"");

            if (!string.IsNullOrWhiteSpace(logFolderPath))
            {
                argsBuilder.Append($" {LOG_FOLDER_PATH_ARG}=\"{logFolderPath}\"");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = executeAssembly,
                Arguments = argsBuilder.ToString(),
                UseShellExecute = true
            });

            Task.Run(() =>
            {
                MonitorStartup(callerAppName);
            });
        }
    }

    public static void MonitorStartup(string callerAppName)
    {
        using var server = new NamedPipeServerStream($"{APP_NAME}Server_" + callerAppName, PipeDirection.In);
        server.WaitForConnection();

        using var reader = new StreamReader(server);

        var message = reader.ReadLine();
        if (Enum.TryParse<AppStatus>(message, out var status) && status == AppStatus.Ready)
        {
            Debug.WriteLine($"{APP_NAME} is ready for {callerAppName}.");
        }
    }

    public static void NotifyReady(string callerAppName)
    {
        using var client = new NamedPipeClientStream(".", $"{APP_NAME}Server_" + callerAppName, PipeDirection.Out);
        client.Connect(3000);
        using var writer = new StreamWriter(client) { AutoFlush = true };
        writer.WriteLine(AppStatus.Ready.ToString());
    }

    public static void Stop(string callerAppName)
    {
        var processes = Process.GetProcessesByName(APP_NAME);
        var mutexName = $"{APP_NAME}_{callerAppName}";
        foreach (var process in processes)
        {
            try
            {
                string cmdLine = GetCommandLine(process);
                if (cmdLine.Contains($"--caller=\"{callerAppName}\"", StringComparison.OrdinalIgnoreCase))
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }

    private static string GetCommandLine(Process process)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");

            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["CommandLine"]?.ToString() ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

        return string.Empty;
    }
}
