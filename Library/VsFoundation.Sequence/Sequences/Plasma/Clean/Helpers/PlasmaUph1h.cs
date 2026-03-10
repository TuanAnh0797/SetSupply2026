namespace VsFoundation.Sequence.Sequences.Plasma.Clean.Helpers;

public static class PlasmaUph1h
{
    public static int CurrentUph1h { get; private set; }
    private static readonly TimeSpan Window1H = TimeSpan.FromHours(1);
    private static readonly Queue<DateTime> _eventsUtc = new();
    private static readonly object _lock = new();
    private static readonly System.Timers.Timer _timer = new(1000);

    static PlasmaUph1h()
    {
        _timer.Elapsed += (_, __) => Update();
        _timer.AutoReset = true;
        _timer.Start();
    }

    public static void RecordOne()
    {
        lock (_lock)
        {
            _eventsUtc.Enqueue(DateTime.UtcNow);
        }
    }

    private static void Update()
    {
        var cutoff = DateTime.UtcNow - Window1H;
        lock (_lock)
        {
            while (_eventsUtc.Count > 0 && _eventsUtc.Peek() < cutoff)
                _eventsUtc.Dequeue();

            CurrentUph1h = _eventsUtc.Count;
        }
    }
}
