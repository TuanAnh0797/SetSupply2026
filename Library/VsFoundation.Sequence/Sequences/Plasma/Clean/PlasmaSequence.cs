using VsFoundation.Base.Constants.Sequence;
using VsFoundation.Base.DI.Sequence;
using VsFoundation.Base.Logger.Interfaces;
using VsFoundation.Sequence.Bases;
using VsFoundation.Sequence.Manager;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Configs;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Constants;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Helpers;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Interfaces;

namespace VsFoundation.Sequence.Sequences.Plasma.Clean;

public class PlasmaSequence : BaseSequence
{
    private enum eStep
    {
        Idle = -1,
        Start,
        RfDelayWait,
        CheckCleanable,
        RfOn,
        CheckRfLevel,
        RfLevelOk,
        RfLevelNg,
        CheckInterlocks,      // over-press, rf, gas, valve
        OverPressOk,
        OverPressNg,
        CycleNextStep,
        SoftStartInit, SoftStartSet, SoftStartTick, SoftStartCheck,
        SoftEndInit, SoftEndSet, SoftEndTick, SoftEndCheck,
        GaugeFault,
        End
    }

    private readonly PlasmaConfig _cfg;
    private IRecipeProvider _recipe;
    private readonly ICimReporter _cim;
    private readonly IRFGenerator _rf;
    private readonly IVacuumGauge _vac;
    private readonly IGas _gas;
    private readonly IVacuumValve _vacPump;
    private readonly IChamber _chamber;

    private ITimerEntry _pwrCheckDelayTimer;
    private ITimerEntry _rfOnTimer;
    private ITimerEntry _stepTimer;
    private ITimerEntry _vacErrOnProcessTimer;
    private ITimerEntry _gasErrOnProcessTimer;
    private ITimerEntry _rfErrOnProcessTimer;
    private ITimerEntry _softPowerTimer;

    private readonly ILoggingService _logger;

    private long _accCleanMs;
    private int _stepTime = 0;
    private int _softStepCountSV;
    private int _softStepCountPV;
    private int _softPowerVal;
    private Action<int> _plasmaCountForOneHour;
    private Action<ePlasmaProcess> _plasmaActionProcess;

    public override int ModuleId { get; set; } = (int)eSequence.PLASMA;
    public override string ModuleName => "PLASMA RF";
    public override string LogHead => "PLASMA_RF";

    public PlasmaSequence(ILoggingService logger, ITimerManager timer, PlasmaConfig config, 
        IRecipeProvider recipe, ICimReporter cim, IRFGenerator generator,
        IVacuumGauge vacuumGauge, IGas gas, 
        IVacuumValve vacuum,
        IChamber chamber, Action<int> plasmaCountForOneHour,
        Action<ePlasmaProcess> plasmaActionProcess)
    {
        _cfg = config;
        _recipe = recipe;
        _cim = cim;
        _rf = generator;
        _gas = gas;
        _vacPump = vacuum;
        _chamber = chamber;
        _plasmaCountForOneHour += plasmaCountForOneHour;
        _plasmaActionProcess += plasmaActionProcess;

        GetTimer(timer);
    }

    private void GetTimer(ITimerManager timer)
    {
        _pwrCheckDelayTimer = timer.Get((int)eTimer.PwrCheckDelayTimer);
        _rfOnTimer = timer.Get((int)eTimer.RfOnTimer);
        _stepTimer = timer.Get((int)eTimer.StepTimer);
        _vacErrOnProcessTimer = timer.Get((int)eTimer.VacErrOnProcessTimer);
        _gasErrOnProcessTimer = timer.Get((int)eTimer.GasErrOnProcessTimer);
        _rfErrOnProcessTimer = timer.Get((int)eTimer.RfErrOnProcessTimer);
        _softPowerTimer = timer.Get((int)eTimer.SoftPowerTimer);
    }

    protected override void SetAlarm(int nErrorCode)
    {
        _plasmaActionProcess?.Invoke(ePlasmaProcess.Error);
        _logger.LogError(string.Format("{0}: Error No.{1} Step {2}", LogHead, nErrorCode, _currentStep));
        base.SetAlarm(nErrorCode);
    }

    public override bool IsReadySeq()
    {
        if (FlagManager.AllRunning()) return false;
        if (!PlasmaDevice.Instance.IsPlasmaStart()) return false;

        if ((PlasmaDevice.Instance.Get_Process(eProcess.VACUUM_PUMPING)
             || PlasmaDevice.Instance.Get_Process(eProcess.GAS_PURGING))
            && _chamber.IsClosed()
            && !PlasmaDevice.Instance.GetUnload()
            && !PlasmaDevice.Instance.Get_Process(eProcess.PLASMA))
        {
            return true;
        }


        return false;
    }

    public override void AlwaysRun(eSeqState state)
    {
        base.AlwaysRun(state);

        _plasmaCountForOneHour?.Invoke(PlasmaUph1h.CurrentUph1h);
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

    private void NextStep(int step = -1)
    {
        if (!GetWork()) return;
        base.NextStep(step);
        string log = string.Format("{0}: Sequence Step {1}", LogHead, (eStep)_currentStep);
    }

    public override eSequenceResult RunSequence()
    {
        switch ((eStep)_currentStep)
        {
            case eStep.Start:
                if (!IsReadySeq()) break;
                _plasmaActionProcess?.Invoke(ePlasmaProcess.PlasmaStart);
                if (!_pwrCheckDelayTimer.IsRunning)
                {
                    _pwrCheckDelayTimer.Start();
                    break;
                }

                _logger.LogInfo($"RF Check Delay Start [{_cfg.GasCheckRfOnDelayMs}] ms");
                NextStep((int)eStep.RfDelayWait);
                break;

            case eStep.RfDelayWait:
                if (_pwrCheckDelayTimer.TotalMilliseconds >= _cfg.GasCheckRfOnDelayMs)
                {
                    _logger.LogInfo("RF Check Delay Done");
                    _pwrCheckDelayTimer.Reset();
                    NextStep((int)eStep.CheckCleanable);
                }
                break;

            case eStep.CheckCleanable:
                if (_chamber?.IsCleanableCondition() ?? true)
                {
                    if (_vac.IsGaugeError()) { NextStep((int)eStep.GaugeFault); break; }

                    if (_cfg.SoftStart > 0)
                    {
                        _logger.LogInfo("Soft Start Begin");
                        NextStep((int)eStep.SoftStartInit);
                    }
                    else NextStep((int)eStep.RfOn);
                }
                break;

            case eStep.RfOn:
                if (!_chamber.IsCleanableCondition() ?? true) break;
                if (_vac.IsGaugeError()) { NextStep((int)eStep.GaugeFault); break; }

                _rf.OutputOn();

                _stepTime = (int)_recipe.GetStep(PlasmaDevice.Instance.GetCurrentStep()).CleanTimeSec;

                if (!_rfOnTimer.IsRunning)
                    _rfOnTimer.Start();

                _logger.LogInfo($"RF-On Start, VacValveOpen={_vacPump.IsOpen()}");
                NextStep((int)eStep.CheckRfLevel);
                break;

            case eStep.CheckRfLevel:
                if (!_rf.IsRemoteMode()) { _rf.SetRemoteMode(); break; }

                if (_rf.IsPowerLevelOk() || (_chamber.IsChamberGlowOn() ?? false))
                {
                    NextStep((int)eStep.RfLevelOk);
                }
                else
                {
                    NextStep((int)eStep.RfLevelNg);
                }
                break;

            case eStep.RfLevelOk:
                if (_vac.IsGaugeError()) { NextStep((int)eStep.GaugeFault); break; }

                if (!_stepTimer.IsRunning)
                {
                    _stepTimer.Start();
                    _vacErrOnProcessTimer.Reset();
                    _cim.ReportPlasmaStarted(_cfg.CeidPlasmaStarted);
                    _logger.LogInfo($"RF-On OK [{_rf.GetForwardWatt():F0}] W. Time={_rfOnTimer.TotalMilliseconds} ms");
                    break;
                }

                if (_stepTimer.TotalMilliseconds >= _cfg.RfStableTimeMs)
                {
                    _logger.LogInfo($"RF Stable Done [{_rf.GetForwardWatt():F0}] W");
                    NextStep((int)eStep.CheckInterlocks);
                }
                break;

            case eStep.RfLevelNg:
                if (_vac.IsGaugeError()) { NextStep((int)eStep.GaugeFault); break; }

                if (_rfOnTimer.TotalMilliseconds >= _cfg.RfPowerOnCheckDelayMs)
                {
                    _logger.LogWarning($"RF Timeout F={_rf.GetForwardWatt():F0}W, R={_rf.GetReflectedWatt():F0}W, Glow={_chamber.IsChamberGlowOn()}");
                    _rf.OutputOff();

                    if (_chamber?.IsChamberGlowOn() ?? false)
                    {
                        SetAlarm(_cfg.AlarmRfOnTimeout);
                    }

                    PlasmaDevice.Instance.SetUnload();
                    PlasmaDevice.Instance.Set_Process(eProcess.VENTILATION, false);
                    NextStep((int)eStep.End);
                }
                break;

            case eStep.CheckInterlocks:
                if (!_rf.IsRemoteMode()) { _rf.SetRemoteMode(); break; }

                bool overPress = IsOverPressure();
                bool rfErr = IsRfError();
                bool gasErr = IsGasError();
                bool vacValveClosed = !_vacPump.IsOpen();

                if (!(overPress || rfErr || gasErr || vacValveClosed))
                {
                    NextStep((int)eStep.OverPressOk);
                }
                else
                {
                    NextStep((int)eStep.OverPressNg);
                }
                break;

            case eStep.OverPressOk:
                {
                    var targetMs = _stepTime * 1000;

                    _gas.AllOn();

                    if (_stepTimer.TotalMilliseconds >= targetMs)
                    {
                        _logger.LogInfo($"Clean Time Done [Step:{PlasmaDevice.Instance.GetCurrentStep()}] [{_stepTimer.TotalMilliseconds}] ms");
                        _accCleanMs += (short)_stepTimer.TotalMilliseconds;
                        _stepTimer.Reset();
                        NextStep((int)eStep.CycleNextStep);
                    }
                }
                break;

            case eStep.OverPressNg:
                if (IsRfError())
                {
                    _logger.LogWarning($"RF Error on F={_rf.GetForwardWatt():F0}W, R={_rf.GetReflectedWatt():F0}W");
                    if (_chamber?.IsChamberGlowOn() ?? false)
                    {
                        SetAlarm(_cfg.AlarmRfOnError);
                    }
                    _rfErrOnProcessTimer.Reset();
                }
                else if (IsOverPressure())
                {
                    _logger.LogWarning($"Vacuum Error On [{_vac.Torr():F3}] Torr, Ms={_vacErrOnProcessTimer.TotalMilliseconds}");
                    SetAlarm(_cfg.AlarmOverPressure);
                    _vacErrOnProcessTimer.Reset();
                }
                else if (IsGasError())
                {
                    _logger.LogWarning("Gas Level Error On");
                    _gasErrOnProcessTimer.Reset();
                }

                _rf.OutputOff();
                //_dev.SetCleanState((int)CLEANING_STATE.PM_READY);
                PlasmaDevice.Instance.SetUnload();
                PlasmaDevice.Instance.Set_Process(eProcess.VENTILATION, false);
                NextStep((int)eStep.End);
                break;

            case eStep.CycleNextStep:
                if (PlasmaDevice.Instance.GetCurrentStep() < _recipe.StepCount - 1)
                {
                    if (!_vacPump.IsOpen())
                    {
                        _vacPump.SetOpen(true);
                        break;
                    }

                    PlasmaDevice.Instance.IncreaseStep();
                    _gas.AllOn();
                    _rf.OutputOn();
                    PlasmaDevice.Instance.ResetGasReady();
                    PlasmaDevice.Instance.Set_Process(eProcess.VACUUM_PUMPING, false);
                    _stepTimer.Reset();
                    NextStep((int)eStep.CheckRfLevel);
                    break;
                }

                if (_cfg.SoftEnd > 0)
                {
                    _logger.LogInfo("Soft End Begin");
                    NextStep((int)eStep.SoftEndInit);
                    break;
                }
                PlasmaDevice.Instance.ResetCurrentStep();
                _accCleanMs = 0;
                _cim.ReportPlasmaCompleted(_cfg.CeidPlasmaCompleted);
                PlasmaDevice.Instance.SetFlagVentilationAutoDoor(false);
                PlasmaDevice.Instance.SetUnload();
                PlasmaDevice.Instance.Set_Process(eProcess.VENTILATION, false);
                PlasmaUph1h.RecordOne();
                NextStep((int)eStep.End);
                break;

            case eStep.GaugeFault:
                if (_chamber?.IsCleanProcStopped() ?? false)
                {
                    SetAlarm(_cfg.AlarmVacGaugeFault);
                }
                break;

            // --- Soft Start/End ---
            case eStep.SoftStartInit:
                InitSoftStepCount(true);
                NextStep((int)eStep.SoftStartSet);
                break;

            case eStep.SoftStartSet:
                if (GetSoftStepValue(true)) break;
                if (!_softPowerTimer.IsRunning)
                {
                    _softPowerTimer.Start();
                    break;
                }
                _rf.SetPowerWatt(_softPowerVal, true);
                NextStep((int)eStep.SoftStartTick);
                break;

            case eStep.SoftStartTick:
                if (_softPowerTimer.TotalMilliseconds < _cfg.SoftTickMs) break;
                _softStepCountPV++;
                NextStep((int)eStep.SoftStartCheck);
                break;

            case eStep.SoftStartCheck:
                if (_softStepCountPV >= _softStepCountSV)
                {
                    _logger.LogInfo("Soft Start End");
                    NextStep((int)eStep.RfOn);
                }
                else
                {
                    _softPowerTimer.Reset();
                    NextStep((int)eStep.SoftStartSet);
                }
                break;

            case eStep.SoftEndInit:
                InitSoftStepCount(false);
                NextStep((int)eStep.SoftEndSet);
                break;

            case eStep.SoftEndSet:
                if (!GetSoftStepValue(false)) break;
                if (!_softPowerTimer.IsRunning)
                {
                    _softPowerTimer.Start();
                    break;
                }
                _rf.SetPowerWatt(_softPowerVal, true);
                NextStep((int)eStep.SoftEndTick);
                break;

            case eStep.SoftEndTick:
                if (_softPowerTimer.TotalMilliseconds < _cfg.SoftTickMs) break;
                _softStepCountPV++;
                NextStep((int)eStep.SoftEndCheck);
                break;

            case eStep.SoftEndCheck:
                if (_softStepCountPV >= _softStepCountSV)
                {
                    _logger.LogInfo("Soft End End");
                    NextStep((int)eStep.CycleNextStep);
                }
                else
                {
                    _softPowerTimer.Reset();
                    NextStep((int)eStep.SoftEndSet);
                }
                break;

            case eStep.End:
                _plasmaActionProcess?.Invoke(ePlasmaProcess.PlasmaEnd);
                PlasmaDevice.Instance.Set_Process(eProcess.PLASMA);
                FlagManager.SetFlag(ModuleId, false);
                NextStep((int)eStep.Start);
                break;
        }

        return eSequenceResult.BUSY;
    }

    private bool IsOverPressure()
    {
        var step = PlasmaDevice.Instance.GetCurrentStep();
        var torr = _vac.Torr();
        var lim = _recipe.GetStep(step).VacuumSetpointTorr + _recipe.OverPressure;

        if (torr > lim)
        {
            if(!_vacErrOnProcessTimer.IsRunning)
            {
                _vacErrOnProcessTimer.Start();
            }  
            else
            {
                if (_vacErrOnProcessTimer.TotalMilliseconds > _recipe.Time)
                    return true;
            }
        }
        return false;
    }

    private bool IsRfError()
    {
        if (GetCleanTime() <= _cfg.RfPowerOnCheckDelayMs) return false;

        if (!_rf.IsPowerLevelOk() || (!_chamber.IsChamberGlowOn() ?? true))
        {
            if(!_rfErrOnProcessTimer.IsRunning)
            {
                _rfErrOnProcessTimer.Start();
            }
            else
            {
                if(_rfErrOnProcessTimer.TotalMilliseconds >= _cfg.RfErrorHoldMs)
                {
                    return true;
                }
            }
        }
        else
        {
            _rfErrOnProcessTimer.Reset();
        }

        return false;
    }

    private bool IsGasError()
    {
        if (GetCleanTime() <= _cfg.GasErrorGuardSec * 1000) return false;

        if (!_gas.IsLevelOk())
        {
            _gasErrOnProcessTimer.Reset();
            return false;
        }
        if(!_gasErrOnProcessTimer.IsRunning)
        {
            _gasErrOnProcessTimer.Start();
            return false;
        }
        if(_gasErrOnProcessTimer.TotalMilliseconds < 1000)
        {
            return false;
        }

        return true;
    }

    private bool InitSoftStepCount(bool start)
    {
        int nInterval = start ? _cfg.SoftStart : _cfg.SoftEnd;

        _softStepCountPV = 0;
        _softStepCountSV = nInterval * 5;
        return _softStepCountSV > 0;
    }

    private bool GetSoftStepValue(bool start)
    {
        var cleanStep = PlasmaDevice.Instance.GetCurrentStep();
        double dbSp = _recipe.GetStep(cleanStep).RfPowerWatt;
        double stepPwr = dbSp / _softStepCountSV;

        if (start)
            _softPowerVal = (int)(stepPwr * _softStepCountPV);
        else
            _softPowerVal = (int)dbSp - (int)(stepPwr * _softStepCountPV);
        return _softPowerVal >= 0;
    }

    private short GetCleanTime() { return (short)(_accCleanMs + GetStepTime()); }
    private short GetStepTime() { return (short)_stepTimer.TotalMilliseconds; }

    public override string GetStepName(int step) => ((eStep)step).ToString();
}
