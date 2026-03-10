using VsLoggerEngine.Constants;
using System.IO;

namespace VsLoggerEngine.Models;

public struct LogItem
{
    public int Index { get; init; }
    public LogLevel LogLevel { get; init; }
    public DateTime LogTime { get; set; }
    public string Prefix { get; init; } = string.Empty;
    public string LogMessage { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; }
    public string FunctionName { get; init; } = string.Empty;
    public int Line { get; init; }

    public LogItem(
        int index,
        LogLevel logLevel,
        DateTime logTime,
        string prefix,
        string logMessage,
        string filePath,
        string functionName,
        int line = 0
    )
    {
        Index = index;
        LogLevel = logLevel;
        LogTime = logTime;
        Prefix = prefix;
        LogMessage = logMessage;
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        FunctionName = functionName;
        Line = line;
    }
}
