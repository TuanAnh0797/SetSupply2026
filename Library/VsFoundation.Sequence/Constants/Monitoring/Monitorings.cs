using System.Collections.Concurrent;
using VsFoundation.Base.DI.Sequence;

namespace VsFoundation.Sequence.Constants.IO.Monitoring;

public class BitStateMonitor<TKey>
{
    private readonly IEnumerable<TKey> _allKeys;
    private readonly Func<TKey, bool> _readValue;
    private readonly Action<TKey, bool> _onChanged;
    private readonly Dictionary<TKey, bool> _prev = new();

    public BitStateMonitor(IEnumerable<TKey> allKeys,
                       Func<TKey, bool> readValue,
                       Action<TKey, bool> onChanged)
    {
        _allKeys = allKeys;
        _readValue = readValue;
        _onChanged = onChanged;
    }

    /// <summary>Call continuously to detect changes.</summary>
    public void Tick()
    {
        foreach (var key in _allKeys)
        {
            bool cur = _readValue(key);
            if (!_prev.TryGetValue(key, out var prev) || prev != cur)
            {
                _prev[key] = cur;
                _onChanged?.Invoke(key, cur);
            }
        }
    }

    public void Reset() => _prev.Clear();
}

public sealed class NumericStateMonitor<TKey>
{
    private readonly IEnumerable<TKey> _allKeys;
    private readonly Func<TKey, double> _readValue;
    private readonly Action<TKey, double> _onChanged;

    private readonly double _absTolerance;
    private readonly double _relTolerance;
    private readonly Dictionary<TKey, double> _prev = new();
    DateTime logTime = new();

    public NumericStateMonitor(IEnumerable<TKey> allKeys,
                               Func<TKey, double> readValue,
                               Action<TKey, double> onChanged,
                               double absTolerance = 0.0,
                               double relTolerance = 0.0)
    {
        _allKeys = allKeys;
        _readValue = readValue;
        _onChanged = onChanged;

        _absTolerance = absTolerance;
        _relTolerance = relTolerance;
    }

    public void Reset() => _prev.Clear();

    public void Tick()
    {
        foreach (var key in _allKeys)
        {
            double cur = _readValue(key);
            if (cur.ToString() == double.NaN.ToString())
                cur = 0;

            if (!_prev.TryGetValue(key, out var prev))
            {
                _prev[key] = cur;
                _onChanged?.Invoke(key, cur);
                continue;
            }

            double delta = Math.Abs(cur - prev);
            bool absOk = delta >= _absTolerance;
            bool relOk = _relTolerance > 0
                         && (Math.Abs(prev) > double.Epsilon)
                         && (delta / Math.Abs(prev) >= _relTolerance);

            if (absOk || relOk)
            {
                _prev[key] = cur;
                _onChanged?.Invoke(key, cur);
            }
        }
    }
}
