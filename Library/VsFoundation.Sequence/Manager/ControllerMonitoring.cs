using System.Diagnostics;
using VsFoundation.Base.DI.Sequence;
using VsFoundation.Base.Helper;
using VsFoundation.Sequence.Constants.IO.Monitoring;
using VSLibrary.Common.MVVM.Core;
using VSLibrary.Controller;

namespace VsFoundation.Sequence.Manager
{
    public class ControllerMonitoring<TDI, TDO, TAI, TAO> : IControllerMonitoring<TDI, TDO, TAI, TAO>
            where TDI : struct, Enum
            where TDO : struct, Enum
            where TAI : struct, Enum
            where TAO : struct, Enum
    {
        #region Digital I/O
        private readonly Dictionary<TDI, IDigitalIOData> _diMap = new();
        private readonly Dictionary<TDO, IDigitalIOData> _doMap = new();
        private BitStateMonitor<TDI> _diMonitor;
        private BitStateMonitor<TDO> _doMonitor;
        private readonly IReadOnlyDictionary<string, IDigitalIOData> _dio;
        private event Action<TDI, bool>? _notifyInInternal;
        private event Action<TDO, bool>? _notifyOutInternal;

        public event Action<TDI, bool> NotifyChangeInBits
        {
            add { _notifyInInternal += value; ReplayCurrentDITo(value); }
            remove { _notifyInInternal -= value; }
        }

        public event Action<TDO, bool> NotifyChangeOutBits
        {
            add { _notifyOutInternal += value; ReplayCurrentDOTo(value); }
            remove { _notifyOutInternal -= value; }
        }
        #endregion

        #region Analog I/O
        private readonly IReadOnlyDictionary<string, IAnalogIOData> _aio;
        private readonly Dictionary<TAI, IAnalogIOData> _aiMap = new();
        private readonly Dictionary<TAO, IAnalogIOData> _aoMap = new();
        private NumericStateMonitor<TAI> _aiMonitor;
        private NumericStateMonitor<TAO> _aoMonitor;
        private event Action<TAI, double>? _notifyAiInternal;
        private event Action<TAO, double>? _notifyAoInternal;

        public event Action<TAI, double> NotifyChangeAnalogIn
        {
            add { _notifyAiInternal += value; ReplayCurrentAITo(value); }
            remove { _notifyAiInternal -= value; }
        }

        public event Action<TAO, double> NotifyChangeAnalogOut
        {
            add { _notifyAoInternal += value; ReplayCurrentAOTo(value); }
            remove { _notifyAoInternal -= value; }
        }
        #endregion

        private readonly ISequenceManager _sequenceManager;
        private readonly ControllerManager _controllerManager;

        private readonly double _aiAbsTol, _aoAbsTol, _aiRelTol, _aoRelTol;

        public ControllerMonitoring(
            double aiAbsTolerance = 0.01, double aoAbsTolerance = 0.01,
            double aiRelTolerance = 0.0, double aoRelTolerance = 0.0)
        {
            _aiAbsTol = aiAbsTolerance; _aoAbsTol = aoAbsTolerance;
            _aiRelTol = aiRelTolerance; _aoRelTol = aoRelTolerance;

            _sequenceManager = VSContainer.Instance.Resolve<ISequenceManager>();
            _controllerManager = VSContainer.Instance.Resolve<ControllerManager>();

            if (_sequenceManager is null || _controllerManager is null)
                throw new InvalidOperationException("SequenceManager/ControllerManager not resolve from VSContainer.");

            _dio = _controllerManager.DIOData;
            _aio = _controllerManager.AIOData;

            _sequenceManager.AlwayRunAction += () =>
            {
                _diMonitor?.Tick();
                _doMonitor?.Tick();
                _aiMonitor?.Tick();
                _aoMonitor?.Tick();
            };

            Initialize();
        }

        public void ResetAndReplay()
        {
            _diMonitor.Reset();
            _doMonitor.Reset();
            _aiMonitor.Reset();
            _aoMonitor.Reset();

            _diMonitor.Tick();
            _doMonitor.Tick();
            _aiMonitor.Tick();
            _aoMonitor.Tick();
        }

        private void Initialize()
        {
            // --------- DIO ----------
            foreach (var di in Enum.GetValues<TDI>())
            {
                var wire = di.GetDescription();
                if (_dio.TryGetValue(wire, out var io) && io.IOType == IOType.InPut)
                    _diMap[di] = io;
            }

            foreach (var @do in Enum.GetValues<TDO>())
            {
                var wire = @do.GetDescription();
                if (_dio.TryGetValue(wire, out var io) && io.IOType == IOType.OUTPut)
                    _doMap[@do] = io;
            }

            var missingDI = Enum.GetValues<TDI>().Except(_diMap.Keys).ToList();
            var missingDO = Enum.GetValues<TDO>().Except(_doMap.Keys).ToList();
            if (missingDI.Count > 0) Debug.WriteLine($"[WARN] Missing DI: {string.Join(", ", missingDI)}");
            if (missingDO.Count > 0) Debug.WriteLine($"[WARN] Missing DO: {string.Join(", ", missingDO)}");

            _diMonitor = new BitStateMonitor<TDI>(
                Enum.GetValues<TDI>(),
                key => _diMap.TryGetValue(key, out var io) && io.Value,
                OnNotifyChangeInBits);

            _doMonitor = new BitStateMonitor<TDO>(
                Enum.GetValues<TDO>(),
                key => _doMap.TryGetValue(key, out var io) && io.Value,
                OnNotifyChangeOutBits);

            // --------- AIO ----------
            foreach (var ai in Enum.GetValues<TAI>())
            {
                var wire = ai.GetDescription();
                if (_aio.TryGetValue(wire, out var io) && io.IOType == IOType.InPut)
                    _aiMap[ai] = io;
            }

            foreach (var ao in Enum.GetValues<TAO>())
            {
                var wire = ao.GetDescription();
                if (_aio.TryGetValue(wire, out var io) && io.IOType == IOType.OUTPut)
                    _aoMap[ao] = io;
            }

            _aiMonitor = new NumericStateMonitor<TAI>(
                Enum.GetValues<TAI>(),
                key => _aiMap.TryGetValue(key, out var io) ? io.AValue : double.NaN,
                OnNotifyChangeAI,
                absTolerance: _aiAbsTol,
                relTolerance: _aiRelTol);

            _aoMonitor = new NumericStateMonitor<TAO>(
                Enum.GetValues<TAO>(),
                key => _aoMap.TryGetValue(key, out var io) ? io.AValue : double.NaN,
                OnNotifyChangeAO,
                absTolerance: _aoAbsTol,
                relTolerance: _aoRelTol);
        }

        #region DIO handlers
        private void OnNotifyChangeInBits(TDI key, bool value) => _notifyInInternal?.Invoke(key, value);
        private void OnNotifyChangeOutBits(TDO key, bool value) => _notifyOutInternal?.Invoke(key, value);

        public void ReplayAllCurrentStates()
        {
            foreach (var (key, io) in _diMap) _notifyInInternal?.Invoke(key, io.Value);
            foreach (var (key, io) in _doMap) _notifyOutInternal?.Invoke(key, io.Value);
        }

        private void ReplayCurrentDITo(Action<TDI, bool> handler)
        {
            if (_diMap.Count == 0) return;
            foreach (var (key, io) in _diMap) handler(key, io.Value);
        }

        private void ReplayCurrentDOTo(Action<TDO, bool> handler)
        {
            if (_doMap.Count == 0) return;
            foreach (var (key, io) in _doMap) handler(key, io.Value);
        }
        #endregion

        #region AIO handlers
        private void OnNotifyChangeAI(TAI key, double value) => _notifyAiInternal?.Invoke(key, value);
        private void OnNotifyChangeAO(TAO key, double value) => _notifyAoInternal?.Invoke(key, value);

        private void ReplayCurrentAITo(Action<TAI, double> handler)
        {
            if (_aiMap.Count == 0) return;
            foreach (var (key, io) in _aiMap) handler(key, io.AValue);
        }

        private void ReplayCurrentAOTo(Action<TAO, double> handler)
        {
            if (_aoMap.Count == 0) return;
            foreach (var (key, io) in _aoMap) handler(key, io.AValue);
        }

        public void ReplayAllCurrentAnalog()
        {
            foreach (var (key, io) in _aiMap) _notifyAiInternal?.Invoke(key, io.AValue);
            foreach (var (key, io) in _aoMap) _notifyAoInternal?.Invoke(key, io.AValue);
        }
        #endregion
    }

    public sealed class ControllerMonitoringAdapter<TDI, TDO, TAI, TAO> : IControllerMonitoring
    where TDI : struct, Enum
    where TDO : struct, Enum
    where TAI : struct, Enum
    where TAO : struct, Enum
    {
        private readonly IControllerMonitoring<TDI, TDO, TAI, TAO> _inner;

        public ControllerMonitoringAdapter(IControllerMonitoring<TDI, TDO, TAI, TAO> inner)
        {
            _inner = inner;
        }

        public event Action<Enum, bool> NotifyChangeInBits
        {
            add => _inner.NotifyChangeInBits += (k, v) => value(k, v);
            remove => _inner.NotifyChangeInBits -= (k, v) => value(k, v);
        }

        public event Action<Enum, bool> NotifyChangeOutBits
        {
            add => _inner.NotifyChangeOutBits += (k, v) => value(k, v);
            remove => _inner.NotifyChangeOutBits -= (k, v) => value(k, v);
        }

        public event Action<Enum, double> NotifyChangeAnalogIn
        {
            add => _inner.NotifyChangeAnalogIn += (k, v) => value(k, v);
            remove => _inner.NotifyChangeAnalogIn -= (k, v) => value(k, v);
        }

        public event Action<Enum, double> NotifyChangeAnalogOut
        {
            add => _inner.NotifyChangeAnalogOut += (k, v) => value(k, v);
            remove => _inner.NotifyChangeAnalogOut -= (k, v) => value(k, v);
        }

        public void ResetAndReplay() => _inner.ResetAndReplay();
        public void ReplayAllCurrentStates() => _inner.ReplayAllCurrentStates();
        public void ReplayAllCurrentAnalog() => _inner.ReplayAllCurrentAnalog();
    }
}
