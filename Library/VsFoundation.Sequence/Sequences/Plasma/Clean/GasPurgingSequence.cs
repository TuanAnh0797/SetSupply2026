using VsFoundation.Base.Constants.Sequence;
using VsFoundation.Base.DI.Sequence;
using VsFoundation.Base.Logger.Interfaces;
using VsFoundation.Sequence.Bases;
using VsFoundation.Sequence.Manager;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Configs;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Constants;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Interfaces;

namespace VsFoundation.Sequence.Sequences.Plasma.Clean;

public class GasPurgingSequence : BaseSequence
{
    private enum eStep
    {
        Idle = -1,
        Start,

        SetGasInjection,
        WaitGasInjection,

        CheckGasLevel,
        GasLevelOk,
        GasLevelNg,

        ResetVacStableTimer,

        StartVacStableDelay,
        WaitVacStableDelay,

        End
    }

    private readonly GasPurgingConfig _cfg;
    private readonly IGas _gas;
    private readonly IVacuumValve _vacValve;
    private readonly IVacuumGauge _gauge;
    private readonly IChamber _chamber;

    private ITimerEntry _gasTimer;
    private ITimerEntry _gasStableTimer;
    private ITimerEntry _vacStableTimer;
    private ITimerEntry _gasErrOnProcessTimer;

    private readonly ILoggingService _logger;
    private Action<ePlasmaProcess> _plasmaActionProcess;

    public override string ModuleName => "PLASMA GAS PURGING";
    public override string LogHead => "PLASMA_GAS_PURGING";

    public override int ModuleId { get; set; } = (int)eSequence.GAS_PURGING;

    public GasPurgingSequence(ILoggingService logger, ITimerManager timer,
            GasPurgingConfig cfg,
            IGas gas,
            IVacuumValve vacValve,
            IVacuumGauge gauge,
            IChamber chamber,
            Action<ePlasmaProcess> plasmaActionProcess)
    {
        _cfg = cfg; 
        _gas = gas; 
        _vacValve = vacValve; 
        _gauge = gauge;
        _chamber = chamber;
        _plasmaActionProcess += plasmaActionProcess;

        GetTimer(timer);
    }

    protected override void SetAlarm(int nErrorCode)
    {
        _plasmaActionProcess?.Invoke(ePlasmaProcess.Error);
        _logger.LogError(string.Format("{0}: Error No.{1} Step {2}", LogHead, nErrorCode, _currentStep));
        base.SetAlarm(nErrorCode);
    }

    private void NextStep(int step = -1)
    {
        if (!GetWork()) return;
        base.NextStep(step);
        string log = string.Format("{0}: Sequence Step {1}", LogHead, (eStep)_currentStep);
    }

    private void GetTimer(ITimerManager timer)
    {
        _gasTimer = timer.Get((int)eTimer.GasTimer);
        _gasStableTimer = timer.Get((int)eTimer.GasStableTimer);
        _vacStableTimer = timer.Get((int)eTimer.VacStableTimer);
        _gasErrOnProcessTimer = timer.Get((int)eTimer.GasErrOnProcessTimer);
    }

    public override bool IsReadySeq()
    {
        if (FlagManager.AllRunning()) return false;
        if (!PlasmaDevice.Instance.IsPlasmaStart()) return false;

        if (PlasmaDevice.Instance.Get_Process(eProcess.VACUUM_PUMPING)
            && _chamber.IsClosed()
            && !PlasmaDevice.Instance.Get_Process(eProcess.GAS_PURGING))
        {
            return true;
        }

        return false;
    }

    public override void Start()
    {
        if (_currentStep < 0)
            _currentStep = 0;
        StepChanged?.Invoke(_currentStep, GetStepName);

        _work = true;
        FlagManager.SetFlag(ModuleId, true);
    }

    public override void Stop()
    {
        _work = false;
        FlagManager.SetFlag(ModuleId, false);

        foreach (var unit in actionUnitStep)
        {
            unit.Value.Cancel();
        }
    }

    public override eSequenceResult RunSequence()
    {
        switch((eStep)_currentStep)
        {
            case eStep.Start:
                if (!IsReadySeq()) break;
                _plasmaActionProcess?.Invoke(ePlasmaProcess.GasInjection);
                if (PlasmaDevice.Instance.IsGasReady())
                {
                    NextStep((int)eStep.ResetVacStableTimer);
                    break;
                }
                NextStep((int)eStep.SetGasInjection);
                break;

            case eStep.SetGasInjection:
                _gasTimer.Reset();
                _gasStableTimer.Reset();
                _gas.AllOn();

                NextStep((int)eStep.WaitGasInjection);
                break;

            case eStep.WaitGasInjection:
                if (_gas.IsAllOn())
                {
                    _gasTimer.Start();
                    _logger.LogDebug("Gas Injection Start");
                    NextStep((int)eStep.CheckGasLevel);
                }
                break;

            case eStep.CheckGasLevel:
                if (_gas.IsLevelOk())
                {
                    NextStep((int)eStep.GasLevelOk);
                    break;
                }

                NextStep((int)eStep.GasLevelNg);
                break;

            case eStep.GasLevelNg:
                if (_gasTimer.TotalMilliseconds < _cfg.GasErrorTimeMs) break;

                if (!_gasErrOnProcessTimer.IsRunning)
                {
                    _gasErrOnProcessTimer.Start();
                    _logger.LogWarning("[GasPurging] Gas Level NG (start pulsing log)");
                }
                else if (_gasErrOnProcessTimer.TotalMilliseconds >= _cfg.GasErrorPulseMs)
                {
                    _logger.LogWarning("[GasPurging] Gas Level NG -> Stop gases & retry");
                    _gas.AllOff();
                    PlasmaDevice.Instance.SetGasError();
                    _gasTimer.Reset();
                    NextStep((int)eStep.Start);
                }
                break;

            case eStep.GasLevelOk:
                if (!_gasStableTimer.IsRunning)
                {
                    _gasStableTimer.Start();
                    var g1 = _gas.ReadGas(0);
                    var g2 = _gas.ReadGas(1);
                    _logger.LogInfo($"Gas Level OK (Gas1:{g1:F1}, Gas2:{g2:F1}) " +
                              $"StableTimer Start (Sp:{_cfg.GasOnCheckDelayMs} ms)");
                    break;
                }

                if (_gasStableTimer.TotalMilliseconds < _cfg.GasOnCheckDelayMs) break;

                PlasmaDevice.Instance.SetGasReady();

                if (_gauge.IsLevelOk(true))
                {
                    _logger.LogInfo($"Gas & Vacuum Stable OK ({_gasStableTimer.TotalMilliseconds} ms)");
                    NextStep((int)eStep.ResetVacStableTimer);
                    break;
                }

                if (_gasStableTimer.TotalMilliseconds >= _cfg.VacReachingTimeMs)
                {
                    _vacValve.SetOpen(false);
                    _logger.LogWarning($"Vacuum Error [{_gauge.Torr():F3}] Torr, " +
                              $"[{_gasStableTimer.TotalMilliseconds}] ms");
                    PlasmaDevice.Instance.SetGasError();
                    _gasStableTimer.Reset();
                    SetAlarm(_cfg.AlarmVacuumTimeOver);
                    PlasmaDevice.Instance.Set_Process(eProcess.VACUUM_PUMPING, false);
                    PlasmaDevice.Instance.SetVacTimeOver();
                    NextStep((int)eStep.End);
                }
                break;

            case eStep.ResetVacStableTimer:
                if (!_vacStableTimer.IsRunning)
                {
                    NextStep((int)eStep.StartVacStableDelay);
                }
                _vacStableTimer.Reset();
                break;

            case eStep.StartVacStableDelay:
                if (!_vacStableTimer.IsRunning)
                {
                    _vacStableTimer.Start();
                    break;
                }
                _logger.LogInfo($"Vacuum Stable Timer Start [{_cfg.VacStableTimeMs}] ms");
                NextStep((int)eStep.WaitVacStableDelay);
                break;

            case eStep.WaitVacStableDelay:
                if (_vacStableTimer.TotalMilliseconds >= _cfg.VacStableTimeMs)
                {
                    _logger.LogInfo("Vacuum Stable Time On");
                    _vacStableTimer.Reset();
                    NextStep((int)eStep.End);
                }
                break;

            case eStep.End:
                if (!PlasmaDevice.Instance.IsGasError())
                {
                    PlasmaDevice.Instance.Set_Process(eProcess.PLASMA, false);
                }

                PlasmaDevice.Instance.Set_Process(eProcess.GAS_PURGING);
                FlagManager.SetFlag(ModuleId, false);
                NextStep((int)eStep.Start);
                return eSequenceResult.SUCCESS;
        }

        return eSequenceResult.BUSY;
    }

    public override string GetStepName(int step) => ((eStep)step).ToString();
}
