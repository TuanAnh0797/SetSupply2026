using VsFoundation.Base.Constants.Sequence;
using VsFoundation.Base.DI.Sequence;
using VsFoundation.Base.Logger.Interfaces;
using VsFoundation.Sequence.Bases;
using VsFoundation.Sequence.Manager;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Configs;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Constants;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Interfaces;
using static VsFoundation.Sequence.Sequences.Plasma.Clean.LeakCheck.LeakCheckSequence;

namespace VsFoundation.Sequence.Sequences.Plasma.Clean;

public class VacuumPumpingSequence : BaseSequence
{
    private enum Step
    {
        Idle = -1,
        Start,
        SetPumpOn,
        WaitPumpOn,
        OpenVacValve,
        WaitVacValveOpen,
        ReportStartToCim,
        WaitReportAck,
        OpenGaugeValveIfAllowed,
        WaitGaugeValveOpen,
        BoosterOnIfNeeded,
        WaitBoosterOn,
        CheckVacuum,
        GaugeFault,
        End
    }

    private readonly VacuumConfig _cfg;
    private readonly IRecipeProvider _cleaningStep;
    private readonly IChamber _chamber;
    private readonly IDryPump _pump;
    private readonly IBoosterPump _booster;
    private readonly IVacuumValve _vacValve;
    private readonly IGaugeValve _gaugeValve;
    private readonly IVacuumGauge _gauge;
    private readonly ICimReporter _cim;

    public override int ModuleId { get; set; } = (int)eSequence.VACUUM_PUMPING;
    public override string ModuleName => "PLASMA VACUUM PUMPING";
    public override string LogHead => "PLASMA_VACUUM_PUMPING";

    private readonly ILoggingService _logger;
    private ITimerEntry _vacTimer;
    private Action<ePlasmaProcess> _plasmaActionProcess;

    public VacuumPumpingSequence(ILoggingService logger, ITimerManager timer, VacuumConfig cfg, IChamber chamber, 
        IDryPump pump, IBoosterPump booster, IVacuumValve vacValve, 
        IGaugeValve gaugeValve, IVacuumGauge gauge, ICimReporter cim, IRecipeProvider recipe,
        Action<ePlasmaProcess> plasmaActionProcess)
    {
        _cfg = cfg;
        _chamber = chamber;
        _pump = pump;
        _booster = booster;
        _vacValve = vacValve;
        _gaugeValve = gaugeValve;
        _gauge = gauge;
        _cim = cim;
        _cleaningStep = recipe;
        _plasmaActionProcess += plasmaActionProcess;

        GetTimer(timer);
    }

    private void GetTimer(ITimerManager timer)
    {
        _vacTimer = timer.Get((int)eTimer.VacTimer);
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

    public override bool IsReadySeq()
    {
        if (FlagManager.AllRunning()) return false;
        if (!PlasmaDevice.Instance.IsPlasmaStart()) return false;

        if ((PlasmaDevice.Instance.IsVacTimeOver() || !PlasmaDevice.Instance.IsGasReady())
        && _chamber.IsClosed()
        && !PlasmaDevice.Instance.Get_Process(eProcess.VACUUM_PUMPING))
        {
            return true;
        }

        return false;
    }

    public override eSequenceResult RunSequence()
    {
        switch ((Step)_currentStep)
        {
            case Step.Start:
                if (!IsReadySeq()) break;
                _plasmaActionProcess?.Invoke(ePlasmaProcess.VacuumPumping);
                if (_pump.NeedOilChange())
                {
                    SetAlarm(_cfg.AlarmPumpOilCheckPm1);
                    return eSequenceResult.FAILD;
                }
                PlasmaDevice.Instance.ResetVacTimeOver();
                PlasmaDevice.Instance.Set_Process(eProcess.VENTILATION, false);
                NextStep((int)Step.SetPumpOn);
                break;

            case Step.SetPumpOn:
                _pump.SetOn(true);
                NextStep((int)Step.WaitPumpOn);
                break;

            case Step.WaitPumpOn:
                if (_pump.IsOn(_cfg.DryPumpMinOnDelayMs))
                    NextStep((int)Step.OpenVacValve);
                break;

            case Step.OpenVacValve:
                _vacValve.SetOpen(true);
                NextStep((int)Step.WaitVacValveOpen);
                break;

            case Step.WaitVacValveOpen:
                if (_vacValve.IsOpen())
                    NextStep((int)Step.ReportStartToCim);
                break;

            case Step.ReportStartToCim:
                _vacTimer.Start();
                _cim.ReportVacuumStarted(_cfg.CeidVacuumStart);
                NextStep((int)Step.WaitReportAck);
                break;

            case Step.WaitReportAck:
                NextStep((int)Step.OpenGaugeValveIfAllowed);
                break;

            case Step.OpenGaugeValveIfAllowed:
                if (_gaugeValve.CheckCoditionGaugeValOpen(PlasmaDevice.Instance.GetCurrentStep()))
                {
                    _gaugeValve.SetOpen(true);
                    NextStep((int)Step.WaitGaugeValveOpen);
                    break;
                }
                _vacValve.SetOpen(true);
                break;

            case Step.WaitGaugeValveOpen:
                if (_gaugeValve.IsOpen())
                    NextStep((int)Step.BoosterOnIfNeeded);
                break;

            case Step.BoosterOnIfNeeded:
                if (_booster.IsUsed() && _gauge.Torr() < _cfg.BoosterEnableBelowTorr)
                {
                    _booster.SetOn(true);
                    NextStep((int)Step.WaitBoosterOn);
                }
                else if(!_booster.IsUsed())
                {
                    NextStep((int)Step.CheckVacuum);
                }
                break;

            case Step.WaitBoosterOn:
                if (_booster.IsOn())
                    NextStep((int)Step.CheckVacuum);
                break;

            case Step.CheckVacuum:
                var torr = _gauge.Torr();
                if (torr < _cfg.GaugeFaultTorrThreshold)
                {
                    NextStep((int)Step.GaugeFault);
                    break;
                }

                if (_gauge.IsLevelOk() || PlasmaDevice.Instance.GetCurrentStep() > 0)
                {
                    _cim.ReportVacuumCompleted(_cfg.CeidVacuumCompleted);
                    _logger.LogInfo($"Vacuum OK [{torr:F3}] Torr, [{_vacTimer.TotalMilliseconds}] ms");
                    NextStep((int)Step.End);
                    break;
                }

                if (_vacTimer.TotalMilliseconds >= _cfg.VacuumReachTimeoutMs)
                {
                    _vacValve.SetOpen(false);
                    _logger.LogWarning($"Vacuum TIMEOUT [{torr:F3}] Torr, [{_vacTimer.TotalMilliseconds}] ms");
                    SetAlarm(_cfg.AlarmVacuumTimeOver);
                    NextStep((int)Step.Start);
                }
                break;

            case Step.GaugeFault:
                if (_chamber.IsCleanProcStopped() ?? false)
                {
                    SetAlarm(_cfg.AlarmVacGaugeFault);
                }

                break;

            case Step.End:
                PlasmaDevice.Instance.Set_Process(eProcess.GAS_PURGING, false); // Clear Process GasPurging
                PlasmaDevice.Instance.Set_Process(eProcess.VACUUM_PUMPING); // Set Process GasPurging
                FlagManager.SetFlag(ModuleId, false);
                NextStep((int)Step.Start);
                return eSequenceResult.SUCCESS;
        }

        return eSequenceResult.BUSY;
    }

    public override string GetStepName(int step) => ((eStep)step).ToString();
}
