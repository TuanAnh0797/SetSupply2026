using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;

namespace VsLoggerEngine;

public static class LoggerEngineSetup
{
    public static void Initialize(string dataVersion, string path)
    {
        InitNLogConfig(path);
        Application.VsLoggerEngine.Start($"{dataVersion}", Path.Combine(path, "LOG"));
    }

    public static void Stop(string dataVersion)
    {
        Application.VsLoggerEngine.Stop($"{dataVersion}");
    }

    private static void InitNLogConfig(string path)
    {
        var config = new LoggingConfiguration();

        var debuggerTarget = new DebuggerTarget("debugger")
        {
            Layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss.fff} | ${level:uppercase=true} | ${event-properties:prefix} | ${message} ${exception:format=Message,Type,StackTrace:maxInnerExceptionLevel=5} | ${callsite-fileName} | ${callsite:className=false:methodName=true} | ${callsite-linenumber}",
        };

        var basePath = Path.Combine(path, "LOG");

        var fileTarget = new FileTarget("logfile")
        {
            FileName = Path.Combine(basePath, "VsLog_${date:format=yyyyMMdd}.log"),
            Layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss.fff} | ${level:uppercase=true} | ${event-properties:prefix} | ${message} ${exception:format=Message,Type,StackTrace:maxInnerExceptionLevel=5} | ${callsite-fileName} | ${callsite:className=false:methodName=true} | ${callsite-linenumber}",
            KeepFileOpen = false,
            ConcurrentWrites = true
        };

        config.AddTarget(debuggerTarget);
        config.AddTarget(fileTarget);

        config.AddRuleForAllLevels(debuggerTarget);
        config.AddRuleForAllLevels(fileTarget);

        LogManager.Configuration = config;
    }
}
