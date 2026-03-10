namespace VsLoggerEngine.Extensions;
public static class TimeSpanExtensions
{
    public static TimeSpan TrimToHhMm(this TimeSpan timeSpan)
    {
        return new TimeSpan(timeSpan.Hours, timeSpan.Minutes, 0);
    }
}
