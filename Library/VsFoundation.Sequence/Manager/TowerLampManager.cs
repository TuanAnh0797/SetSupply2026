using System.Collections.Concurrent;
using System.Diagnostics;
using VsFoundation.Base.Constants.TowerLamp;
using VsFoundation.Base.DI.Sequence;
using VsFoundation.Base.Models.TowerLamp;
using VsFoundation.Sequence.Bases.TowerLamp;

namespace VsFoundation.Sequence.Manager;

public class TowerLampManager : ITowerLampManager
{
    private readonly IReadOnlyDictionary<eTowerLamp, IDiscreteOutput> _outputs;
    private TowerSettings _settings;

    private readonly CancellationTokenSource _cts = new();
    private readonly Task _blinkTask;

    private sealed class BlinkState
    {
        public int PeriodMs;
        public int DutyPercent;
        public long EpochMs;
        public bool LastAppliedOn;
    }

    private readonly ConcurrentDictionary<eTowerLamp, BlinkState> _blinks = new();

    private readonly object _applyLock = new();
    private eTowerMode? _currentMode;

    public Action<eTowerLamp, eTowerActionType> TowerLampAction { get; set; }
    public Action<eTowerMode, eTowerActionType> TowerModeAction { get; set; }

    public TowerLampManager(
        IReadOnlyDictionary<eTowerLamp, IDiscreteOutput> outputs,
        TowerSettings settings)
    {
        _outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
        _settings = settings;

        _blinkTask = Task.Run(BlinkLoopAsync);
    }

    public void UpdateSettings(TowerSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        lock (_applyLock) _settings = settings;
        if (_currentMode.HasValue) SetMode(_currentMode.Value);
    }

    public void SetMode(eTowerMode mode)
    {
        if (_settings == null) return;

        List<TowerStep> steps;
        lock (_applyLock)
        {
            if (!_settings.Modes.TryGetValue(mode, out steps))
                throw new InvalidOperationException($"No settings for mode {mode}");
        }

        _blinks.Clear();
        foreach (var o in _outputs.Values) o.Off();

        foreach (var s in steps)
        {
            var output = _outputs[s.Target];

            switch (s.Action)
            {
                case eTowerActionType.Off:
                    output.Off();
                    RaiseLamp(s.Target, eTowerActionType.Off);
                    break;

                case eTowerActionType.On:
                    output.On();
                    RaiseLamp(s.Target, eTowerActionType.On);
                    if (s.TimeOnMs > 0)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(s.TimeOnMs, _cts.Token);
                                output.Off();
                                RaiseLamp(s.Target, eTowerActionType.Off);
                            }
                            catch (TaskCanceledException) { }
                        });
                    }
                    break;

                case eTowerActionType.Blink:
                    output.On();
                    _blinks[s.Target] = new BlinkState
                    {
                        PeriodMs = Math.Max(100, s.PeriodMs),
                        DutyPercent = Math.Clamp(s.DutyPercent, 1, 99),
                        EpochMs = Stopwatch.GetTimestamp(),
                        LastAppliedOn = true
                    };
                    RaiseLamp(s.Target, eTowerActionType.Blink);
                    break;
            }
        }
        RaiseMode(mode, eTowerActionType.On);
        _currentMode = mode;
    }


    public void AllOff()
    {
        _blinks.Clear();
        foreach (var kv in _outputs)
        {
            kv.Value.Off();
            RaiseLamp(kv.Key, eTowerActionType.Off);
        }
        _currentMode = null;
    }

    private async Task BlinkLoopAsync()
    {
        var sw = Stopwatch.StartNew();
        while (!_cts.IsCancellationRequested)
        {
            if (_blinks.IsEmpty)
            {
                await Task.Delay(50, _cts.Token).ConfigureAwait(false);
                continue;
            }

            long nowTicks = sw.ElapsedMilliseconds;

            foreach (var kv in _blinks)
            {
                var lamp = kv.Key;
                var st = kv.Value;

                var period = st.PeriodMs;
                var onWindow = period * st.DutyPercent / 100;

                var pos = (int)((nowTicks - st.EpochMs) % period);
                bool shouldOn = pos < onWindow;

                if (shouldOn != st.LastAppliedOn)
                {
                    var outp = _outputs[lamp];
                    if (shouldOn)
                    {
                        outp.On();
                        RaiseLamp(lamp, eTowerActionType.On);
                    }
                    else
                    {
                        outp.Off();
                        RaiseLamp(lamp, eTowerActionType.Off);
                    }
                    st.LastAppliedOn = shouldOn;
                }
            }

            await Task.Delay(20, _cts.Token).ConfigureAwait(false);
        }
    }

    private void RaiseLamp(eTowerLamp lamp, eTowerActionType action)
    {
        var cb = TowerLampAction;
        if (cb == null) return;
        cb(lamp, action);
    }

    private void RaiseMode(eTowerMode mode, eTowerActionType action)
    {
        var cb = TowerModeAction;
        if (cb == null) return;
        cb(mode, action);
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _blinkTask.Wait(200);
        }
        catch { }

        AllOff();
        _cts.Dispose();
    }
}