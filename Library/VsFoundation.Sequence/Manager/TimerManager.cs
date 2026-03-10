using System.Collections.Concurrent;
using System.Diagnostics;
using VsFoundation.Base.DI.Sequence;

namespace VsFoundation.Sequence.Manager;

// Manages the collection of timers. No timing logic here.
public sealed class TimerManager : ITimerManager
{
    private readonly ConcurrentDictionary<int, ITimerEntry> _timers = new();

    /// <summary>Add a new timer with the given id. Returns false if it already exists.</summary>
    public bool Add(int timerId) => _timers.TryAdd(timerId, new TimerEntry());

    /// <summary>Remove a timer by id.</summary>
    public bool Remove(int timerId) => _timers.TryRemove(timerId, out _);

    /// <summary>Remove all timers.</summary>
    public void Clear() => _timers.Clear();

    /// <summary>Get existing timer; throw if not found.</summary>
    public ITimerEntry Get(int timerId)
    {
        return _timers.TryGetValue(timerId, out var t) ? t
           : throw new KeyNotFoundException($"Timer {timerId} not found.");
    }

    /// <summary>Get existing timer or create a new one if missing.</summary>
    public ITimerEntry GetOrAdd(int timerId) => _timers.GetOrAdd(timerId, _ => new TimerEntry());

    /// <summary>Try get existing timer.</summary>
    public bool TryGet(int timerId, out ITimerEntry? entry) => _timers.TryGetValue(timerId, out entry);

    public void ResetAll()
    {
        foreach (var timer in _timers.Values)
        {
            timer.Reset();
        }
    }

    public void Stop()
    {
        foreach(var timer in _timers.Values)
        {
            timer?.Stop();
        }
    }
}

// Encapsulates all timing behavior for a single logical timer.
public sealed class TimerEntry : ITimerEntry
{
    private readonly Stopwatch _sw = new();
    private readonly object _gate = new();

    /// <summary>Start the timer if not already running.</summary>
    public void Start()
    {
        lock (_gate)
        {
            if (!_sw.IsRunning) _sw.Start();
        }
    }

    /// <summary>Stop/pause the timer if running.</summary>
    public void Stop()
    {
        lock (_gate)
        {
            if (_sw.IsRunning) _sw.Stop();
        }
    }

    /// <summary>Reset elapsed time to zero and stop.</summary>
    public void Reset()
    {
        lock (_gate)
        {
            _sw.Reset();
        }
    }

    /// <summary>Reset and start immediately.</summary>
    public void Restart()
    {
        lock (_gate)
        {
            _sw.Reset();
            _sw.Start();
        }
    }

    /// <summary>Returns the current elapsed time.</summary>
    public TimeSpan Elapsed
    {
        get
        {
            lock (_gate)
            {
                return _sw.Elapsed;
            }
        }
    }

    /// <summary>Indicates whether the timer is currently running.</summary>
    public bool IsRunning
    {
        get { lock (_gate) return _sw.IsRunning; }
    }

    public double TotalMilliseconds => Elapsed.TotalMilliseconds;
    public double TotalSeconds => Elapsed.TotalSeconds;
    public double TotalMinutes => Elapsed.TotalMinutes;
}
