using VsFoundation.Sequence.Bases;
using VsFoundation.Sequence.Manager;
using VsFoundation.Base.Logger.Interfaces;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Configs;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Constants;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Interfaces;
using VsFoundation.Base.Constants.Sequence;
using VsFoundation.Base.DI.Sequence;

namespace VsFoundation.Sequence.Sequences.Plasma.Clean;

public class VentilationSequence : BaseSequence
{
    public override bool CanMonitoring => true;

    private enum eStep
    {
        Idle = -1,
        Start,
        SetN2Purging,
        SetGaugeValve,
        CheckGaugePress,
        SetPurgeError,
        SetAutoDoorOpen,
        WaitAutoDoorOpen,
        SetN2PurgeOff,
        SetAutoCool,
        WaitAutoCool,
        End
    }

    private readonly VentilationConfig _cfg;
    private readonly ICimReporter _cim;
    private readonly IDryPump _pump;
    private readonly IBoosterPump _booster;
    private readonly IN2Purge _n2;
    private readonly IGaugeValve _gauge;
    private readonly IAutoDoor _autoDoor;
    private readonly IAutoCooler _cooler;
    private readonly IJobCompleteSink _jobSink;
    private readonly IChamber _chamber;

    public override int ModuleId { get; set; } = (int)eSequence.VENTILATION;
    public override string ModuleName => "PLASMA VENTILATION";
    public override string LogHead => "PLASMA_VENTILATION";
    public override bool CanInitialize => true;

    private ITimerEntry _purgeTimer;
    private ITimerEntry _rfOnTimer;
    private ITimerEntry _ventilationTimer;
    private ITimerEntry _pumpTimer;
    private ITimerManager _timerManager;

    private readonly ILoggingService _logger;
    private eSeqState _state = eSeqState.STOP;
    private Action<ePlasmaProcess> _plasmaActionProcess;

    public VentilationSequence(
            ILoggingService logger,
            ITimerManager timer,
            VentilationConfig cfg,
            ICimReporter cim,
            IDryPump pump,
            IBoosterPump booster,
            IN2Purge n2,
            IGaugeValve gauge,
            IChamber chamber,
            Action<ePlasmaProcess> plasmaActionProcess,
            IJobCompleteSink jobSink = null,
            IAutoDoor autoDoor = null,
            IAutoCooler cooler = null)
    {
        _timerManager = timer;
        _cfg = cfg; 
        _cim = cim;
        _pump = pump; 
        _booster = booster; 
        _n2 = n2; 
        _gauge = gauge; 
        _autoDoor = autoDoor;
        _cooler = cooler;
        _jobSink = jobSink;
        _chamber = chamber;
        _plasmaActionProcess += plasmaActionProcess;

        GetTimer(timer);
    }

    private void GetTimer(ITimerManager timer)
    {
        _purgeTimer = timer.Get((int)eTimer.PurgeTimer);
        _rfOnTimer = timer.Get((int)eTimer.RfOnTimer);
        _ventilationTimer = timer.Get((int)eTimer.VentilationTimer);
        _pumpTimer = timer.Get((int)eTimer.PumpTimer);
    }

    protected override void SetAlarm(int nErrorCode)
    {
        _plasmaActionProcess?.Invoke(ePlasmaProcess.Error);
        _logger.LogError(string.Format("{0}: Error No.{1} Step {2}", LogHead, nErrorCode, _currentStep));
        base.SetAlarm(nErrorCode);
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
        PlasmaDevice.Instance.PlasmaStart();

        foreach (var unit in actionUnitStep)
        {
            unit.Value.Cancel();
        }

        IsInitialized = false;
    }

    private void NextStep(int step = -1)
    {
        base.NextStep(step);
        string log = string.Format("{0}: Sequence Step {1}", LogHead, (eStep)_currentStep);
    }

    public override void AlwaysRun(eSeqState state)
    {
        _state = state;
        if (GetWork()) return;

        Initialize();
    }

    public override void FirstHomeCmd()
    {
        base.FirstHomeCmd();
        _currentStep = (int)eStep.Start;
        PlasmaDevice.Instance.PlasmaStart();
        GetProgress(0);
    }

    public override bool Initialize()
    {
        if (IsInitialized) return true;

        if(_chamber.IsOpen())
        {
            IsInitialized = true;
            GetProgress(100);
            return IsInitialized;
        }

        if(RunSequence() == eSequenceResult.SUCCESS)
        {
            GetProgress(100);
            IsInitialized = true;
        }

        return IsInitialized;
    }

    public override bool IsReadySeq()
    {
        if (!IsInitialized && _chamber.IsClosed())
        {
            return true;
        }
        if (FlagManager.AllRunning()) return false;
        if (!PlasmaDevice.Instance.IsPlasmaStart()) return false;  

        if (PlasmaDevice.Instance.Get_Process(eProcess.PLASMA)
            && PlasmaDevice.Instance.GetUnload()
            && _chamber.IsClosed()
            && !PlasmaDevice.Instance.Get_Process(eProcess.VENTILATION))
        {
            return true;
        }

        return false;
    }

    public override eSequenceResult RunSequence()
    {
        switch((eStep)_currentStep)
        {
            case eStep.Start:
                if (!IsReadySeq()) break;
                _plasmaActionProcess?.Invoke(ePlasmaProcess.Ventilation);
                _ventilationTimer.Start();
                _rfOnTimer.Reset();
                PlasmaDevice.Instance.ResetUnload();

                if (PlasmaDevice.Instance.GetFlagVentilationAutoDoor())
                {
                    NextStep((int)eStep.SetAutoDoorOpen);
                    break;
                }

                _pump.SetOn(false);
                _pumpTimer.Reset();
                if (!_chamber.IsCleanProcStopped(true) ?? true) break;

                PlasmaDevice.Instance.Clear();

                if (_booster.IsOn())
                {
                    _booster.SetOn(false);
                }

                _cim.ReportVacPurgeStarted(_cfg.CeidVacPurgeStarted);
                NextStep((int)eStep.SetN2Purging);
                break;

            case eStep.SetN2Purging:
                if (_n2.IsOn())
                {
                    _purgeTimer.Start();
                    NextStep((int)eStep.SetGaugeValve);
                    break;
                }
                if (!_n2.SetOn())
                {
                    SetAlarm(_cfg.AlarmCannotPurge);
                }
                break;

            case eStep.SetGaugeValve:
                if (_gauge.NeedCloseForPressure())
                {
                    if (_gauge.IsClosed())
                    {
                        NextStep((int)eStep.CheckGaugePress);
                        break;
                    }
                    _gauge.SetOpen(false);
                    break;
                }

                if (_purgeTimer.TotalMilliseconds >= _n2.GetVentTimeMs())
                    NextStep((int)eStep.SetPurgeError);
                break;

            case eStep.CheckGaugePress:
                if (_purgeTimer.TotalMilliseconds < _n2.GetVentTimeMs()) break;

                if (_gauge.IsMaxPressure())
                {
                    NextStep((int)eStep.SetAutoDoorOpen);
                    break;
                }

                NextStep((int)eStep.SetPurgeError);
                break;

            case eStep.SetPurgeError:
                SetAlarm(_cfg.AlarmVacNotPurged);
                _purgeTimer.Reset();
                NextStep((int)eStep.SetAutoDoorOpen);
                break;

            case eStep.SetAutoDoorOpen:
                if (_autoDoor.IsEnabled())
                {
                    _autoDoor.Open();
                    NextStep((int)eStep.WaitAutoDoorOpen);
                    break;
                }
                NextStep((int)eStep.SetN2PurgeOff);
                break;

            case eStep.WaitAutoDoorOpen:
                if (_autoDoor.IsOpened(500))
                {
                    NextStep((int)eStep.SetN2PurgeOff);
                    break;
                }

                if (!Delay(_cfg.AutoDoorTimeoutMs)) break;

                NextStep((int)eStep.SetAutoDoorOpen);
                break;

            case eStep.SetN2PurgeOff:
                if (!_n2.IsOff())
                {
                    _n2.SetOff();
                    break;
                }
                _cim.ReportProcessEnd(_cfg.CeidProcessEnd);
                NextStep((int)eStep.SetAutoCool);
                break;

            case eStep.SetAutoCool:
                if (_cooler.IsUsed()) _cooler.Start();
                NextStep((int)eStep.WaitAutoCool);
                break;

            case eStep.WaitAutoCool:
                if (_cooler.IsUsed() && !_cooler.IsDone()) break;
                NextStep((int)eStep.End);
                break;

            case eStep.End:
                if (GetWork())
                {
                    _chamber.SetOpen();
                    if (!_chamber.IsOpen()) break;
                }

                PlasmaDevice.Instance.PlasmaStart(false);
                _jobSink?.OnJobCompleted(10000);
                _jobSink.IsJobCompleted = true;
                _plasmaActionProcess?.Invoke(ePlasmaProcess.End);

                PlasmaDevice.Instance.Set_Process(eProcess.VENTILATION);
                PlasmaDevice.Instance.Set_Process(eProcess.VACUUM_PUMPING, false);

                _timerManager.ResetAll();

                FlagManager.SetFlag(ModuleId, false);
                NextStep((int)eStep.Start);
                return eSequenceResult.SUCCESS;
        }

        return eSequenceResult.BUSY;
    }


    public override string GetStepName(int step) => ((eStep)step).ToString();
}
