using VsFoundation.Sequence.Bases;
using VsFoundation.Sequence.Manager;
using VsFoundation.Base.Logger.Interfaces;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Configs;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Constants;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Interfaces;
using VsFoundation.Base.Constants.Sequence;
using VsFoundation.Base.Models.Sequence;
using VsFoundation.Base.DI.Sequence;

namespace VsFoundation.Sequence.Sequences.Plasma.Clean.LeakCheck;

public class LeakCheckSequence : BaseSequence
{
    public enum eStep
    {
        Idle = -1,

        Start,

        SetChamberClose,
        WaitChamberClose,

        SetPumpOn,
        WaitPumpOn,

        OpenVacValve,
        WaitVacValveOpen,

        OpenGaugeValveIfAllowed,
        WaitGaugeValveOpen,

        BoosterOnIfNeeded,
        WaitBoosterOn,

        CheckVacuum,
        StableChamber,

        LeakCheck,

        Ventilation,

        End
    }
    public override int ModuleId { get; set; } = (int)eSequence.LEAK_CHECK;

    private readonly ILoggingService _logger;
    private LeakCheckConfig _leakCheckConfig;
    private readonly IRecipeProvider _cleaningStep;
    private readonly IChamber _chamber;
    private readonly IDryPump _pump;
    private readonly IBoosterPump _booster;
    private readonly IVacuumValve _vacValve;
    private readonly IGaugeValve _gaugeValve;
    private readonly IVacuumGauge _VacGauge;
    private readonly IN2Purge _n2Purge;

    private ITimerEntry _overPumpingTime;
    private ITimerEntry _stableTime;
    private ITimerEntry _leakCheckTime;
    private ITimerEntry _purgeTimer;
    private ITimerEntry _elapTimer;

    private LeakCheckData _leakData = new();
    private LeakInfo _leak = new();

    private Action<LeakCheckData> _leakCheckDataAction;
    private Action<LeakInfo> _leakInfo;

    public LeakCheckSequence(ILoggingService logging,ITimerManager timer ,LeakCheckConfig leakCheckConfig,
        IRecipeProvider cleaningStep,
        IChamber chamber, IDryPump pump,
        IBoosterPump booster, IVacuumValve vacValve,
        IGaugeValve gaugeValve, IVacuumGauge vacGauge, IN2Purge n2Purge,
        Action<LeakCheckData> leakCheckDataAction,
        Action<LeakInfo> leakInfo)
    {
        _logger = logging;
        _leakCheckConfig = leakCheckConfig;
        _cleaningStep = cleaningStep;
        _chamber = chamber;
        _pump = pump;
        _booster = booster;
        _vacValve = vacValve;
        _gaugeValve = gaugeValve;
        _VacGauge = vacGauge;
        _n2Purge = n2Purge;
        _leakCheckDataAction += leakCheckDataAction;
        _leakInfo += leakInfo;

        GetTimer(timer);
    }

    private void GetTimer(ITimerManager timer)
    {
        _overPumpingTime = timer.Get((int)eTimer.PumpTimer);
        _stableTime = timer.Get((int)eTimer.VacStableTimer);
        _leakCheckTime = timer.Get((int)eTimer.LeakCheckTimer);
        _purgeTimer = timer.Get((int)eTimer.PurgeTimer);
        _elapTimer = timer.Get((int)eTimer.ElapsedTimer);
    }

    private void ResetController()
    {
        _pump.SetOn(false);
        _gaugeValve.SetOpen(false);
        _vacValve.SetOpen(false);
        if (_booster.IsUsed())
        {
            _booster.SetOn(false);
        }
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

        _leak = new();
    }

    public override bool IsReadySeq()
    {
        if (FlagManager.AllRunning()) return false;
        if (!PlasmaDevice.Instance.IsLeakCheck()) return false;
        if (!PlasmaDevice.Instance.Get_Process(eProcess.LEAK_CHECK)) return false;

        return true;
    }

    public override void AlwaysRun(eSeqState state)
    {
        if (!GetWork()) return;

        _leak.LeakRate = _leakData.CalculateLeakRate();
        _leak.CurrentPressure = _VacGauge.Torr();
        _leak.ElapsedTime = _elapTimer.TotalMilliseconds;

        _leakInfo?.Invoke(_leak);
    }

    public override eSequenceResult RunSequence()
    {
        switch ((eStep)_currentStep)
        {
            case eStep.Start:
                if (!IsReadySeq()) break;
                if (!_elapTimer.IsRunning)
                {
                    _elapTimer.Start();
                }
                    
                ResetController();

                _leak = new();
                _leakData = new();
                NextStep((int)eStep.SetChamberClose);
                break;

            case eStep.SetChamberClose:
                _chamber.SetClose();
                NextStep((int)eStep.WaitChamberClose);
                break;

            case eStep.WaitChamberClose:
                if(_chamber.IsClosed())
                {
                    NextStep((int)eStep.SetPumpOn);
                    break;
                }
                if(Timeout(3000))
                {
                    _leakData.LeakCheckNG = true;
                    _leakData.AlarmMessage = "Cannot close Chamber";

                    _leakCheckDataAction?.Invoke(_leakData);
                    NextStep((int)eStep.Ventilation);
                }
                break;

            case eStep.SetPumpOn:
                _pump.SetOn(true);
                NextStep((int)eStep.WaitPumpOn);
                break;

            case eStep.WaitPumpOn:
                if (_pump.IsOn())
                    NextStep((int)eStep.OpenVacValve);
                break;

            case eStep.OpenVacValve:
                _vacValve.SetOpen(true);
                NextStep((int)eStep.WaitVacValveOpen);
                break;

            case eStep.WaitVacValveOpen:
                if (_vacValve.IsOpen())
                    NextStep((int)eStep.OpenGaugeValveIfAllowed);
                break;

            case eStep.OpenGaugeValveIfAllowed:
                if (_gaugeValve.CheckCoditionGaugeValOpen(PlasmaDevice.Instance.GetCurrentStep()))
                {
                    _gaugeValve.SetOpen(true);
                    NextStep((int)eStep.WaitGaugeValveOpen);
                }
                else
                {
                    _vacValve.SetOpen(true);
                    NextStep((int)eStep.BoosterOnIfNeeded);
                }
                break;

            case eStep.WaitGaugeValveOpen:
                if (_gaugeValve.IsOpen())
                    NextStep((int)eStep.BoosterOnIfNeeded);
                break;

            case eStep.BoosterOnIfNeeded:
                if (_booster.IsUsed() && _VacGauge.Torr() < 9.9)
                {
                    _booster.SetOn(true);
                    NextStep((int)eStep.WaitBoosterOn);
                }
                else if(!_booster.IsUsed())
                {
                    NextStep((int)eStep.CheckVacuum);
                }
                break;

            case eStep.WaitBoosterOn:
                if (_booster.IsOn())
                    NextStep((int)eStep.CheckVacuum);
                break;

            case eStep.CheckVacuum:
                var torr = _VacGauge.Torr();
                var torrCheck = _cleaningStep.GetStep(0);

                if(!_overPumpingTime.IsRunning)
                {
                    _overPumpingTime.Start();
                }
                if (torr <= torrCheck.VacuumSetpointTorr)
                {
                    NextStep((int)eStep.StableChamber);
                    break;
                }
                if(_overPumpingTime.TotalMilliseconds >= _leakCheckConfig.OverPumpingTimeMs)
                {
                    _leakData.AlarmMessage = "Over Pumping";
                    _leakData.LeakCheckNG = true;
                    _leakCheckDataAction?.Invoke(_leakData);
                    NextStep((int)eStep.Ventilation);
                    _overPumpingTime.Reset();
                    break;
                }
                break;

            case eStep.StableChamber:
                if (!_stableTime.IsRunning)
                {
                    _stableTime.Start();
                }
                if(_stableTime.TotalMilliseconds >= _leakCheckConfig.StableTimeMs)
                {
                    NextStep((int)eStep.LeakCheck);
                    _stableTime.Reset();
                }
                break;

            case eStep.LeakCheck:
                if(!_leakCheckTime.IsRunning)
                {
                    _leakData.LeakCheckStartTime = DateTime.Now;
                    _leakData.PressureStart = _VacGauge.Torr();
                    _leakCheckDataAction?.Invoke(_leakData);
                    _leakCheckTime.Start();
                }    
                if(_leakCheckTime.TotalMilliseconds >= _leakCheckConfig.LeakCheckTimeMs)
                {
                    _leakData.LeakCheckEndTime = DateTime.Now;
                    _leakData.PressureEnd = _VacGauge.Torr();

                    if(_leakData.IsLeakRateOver(_leakCheckConfig.LeakAlarmRateTorr))
                    {
                        _leakData.AlarmMessage = "Leak check NG";
                        _leakData.LeakCheckNG = true;
                        break;
                    }

                    _leakCheckDataAction?.Invoke(_leakData);
                    NextStep((int)eStep.Ventilation);
                    _leakCheckTime.Reset();
                }    

                break;

            case eStep.Ventilation:  
                if(!_purgeTimer.IsRunning)
                {
                    ResetController();
                    _n2Purge.SetOn();
                    _purgeTimer.Start();
                }    

                if(_purgeTimer.TotalMilliseconds >= _leakCheckConfig.VentilationTimeMs)
                {
                    _n2Purge.SetOff();
                    _chamber.SetOpen();
                    NextStep((int)eStep.End);
                }    

                break;

            case eStep.End:
                _elapTimer.Reset();
                PlasmaDevice.Instance.LeakCheckStart(false);

                if (_leakData.LeakCheckNG)
                {
                    return eSequenceResult.FAILD;
                }
                Stop();
                return eSequenceResult.SUCCESS;
        }

        return eSequenceResult.BUSY;
    }
}
