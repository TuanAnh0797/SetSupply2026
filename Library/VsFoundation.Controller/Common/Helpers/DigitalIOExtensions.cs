using VSLibrary.Controller;

namespace VsFoundation.Controller.Common.Helpers;

public static class DigitalIOExtensions
{
    private static readonly Dictionary<string, DateTime?> _onStartTimes = new();
    private static readonly Dictionary<string, DateTime?> _offStartTimes = new();

    public static bool IsOn(this IDigitalIOData dio, int timeOn)
    {
        string key = dio.WireName;

        bool current = dio.IsOn();

        if (timeOn <= 0)
            return current;

        if (!_onStartTimes.ContainsKey(key))
            _onStartTimes[key] = null;

        if (current)
        {
            if (_onStartTimes[key] == null)
            {
                _onStartTimes[key] = DateTime.Now;
                return false;
            }

            var duration = DateTime.Now - _onStartTimes[key].Value;

            if (duration.TotalMilliseconds >= timeOn)
            {
                _onStartTimes[key] = null;
                return true;
            }

            return false;
        }
        else
        {
            _onStartTimes[key] = null;
            return false;
        }
    }

    public static bool IsOff(this IDigitalIOData dio, int timeOff)
    {
        string key = dio.WireName;

        bool current = dio.IsOn();

        if (timeOff <= 0)
            return !current;

        if (!_offStartTimes.ContainsKey(key))
            _offStartTimes[key] = null;

        if (!current)
        {
            if (_offStartTimes[key] == null)
            {
                _offStartTimes[key] = DateTime.Now;
                return false;
            }

            var duration = DateTime.Now - _offStartTimes[key].Value;

            if (duration.TotalMilliseconds >= timeOff)
            {
                _offStartTimes[key] = null;
                return true;
            }

            return false;
        }
        else
        {
            _offStartTimes[key] = null;
            return false;
        }
    }


}
