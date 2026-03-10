using VsFoundation.Base.Constants.Sequence;
using VsFoundation.Base.DI.Sequence;
using VsFoundation.Base.Logger.Interfaces;
using VsFoundation.Base.Models.Sequence;
using VsFoundation.Sequence.Manager;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Configs;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Constants;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Interfaces;
using VsFoundation.Sequence.Sequences.Plasma.Clean.LeakCheck;

namespace VsFoundation.Sequence.Sequences.Plasma.Clean;

public class SeqCleaning : ISeqCleaning
{
    private ISequenceManager _sequenceManager;
    private ITimerManager _timerManager;

    private readonly GasPurgingConfig _cfgGas;
    private readonly PlasmaConfig _cfgPlasma;
    private readonly VacuumConfig _cfgVacuum;
    private readonly VentilationConfig _cfgVentilation;
    private readonly LeakCheckConfig _cfgLeakCheck;
    private readonly ICimReporter _cim;
    private readonly IDryPump _pump;
    private readonly IBoosterPump _booster;
    private readonly IN2Purge _n2;
    private readonly IGaugeValve _gaugeValve;
    private readonly IAutoDoor _door;
    private readonly IAutoCooler _cooler;
    private readonly IJobCompleteSink _jobSink;
    private readonly IRecipeProvider _recipe;
    private readonly IChamber _chamber;
    private readonly IVacuumValve _vacValve;
    private readonly IVacuumGauge _vacGauge;
    private readonly IRFGenerator _rf;
    private readonly IGas _gas;
    private readonly ILoggingService _logging;

    public Action<int> PlasmaCountForOneHour { get; set; }
    public Action<ePlasmaProcess> PlasmaActionProcess { get; set; }
    public PlasmaParameterAction PlasmaParameterAction { get; set; } = new();
    public Action<LeakCheckData> LeakCheckDataAction { get; set; }
    public Action<LeakInfo> LeakInfo { get; set; }

    public SeqCleaning(ISequenceManager sequenceManager, ITimerManager timerManager,ILoggingService logging,
        GasPurgingConfig gasConfig, PlasmaConfig plsConfig, VacuumConfig vacuumConfig, VentilationConfig ventilationConfig, LeakCheckConfig leakCheckCfg,
        ICimReporter cim, IDryPump pump, IBoosterPump booster,
        IN2Purge purge, IGaugeValve gaugeValve, IRecipeProvider recipe, IChamber chamber,
        IVacuumValve vacuumValve, IVacuumGauge vacGauge, IRFGenerator generator, IGas gas, IJobCompleteSink jobEnd = null,
        IAutoDoor autoDoor = null, IAutoCooler cool = null)
    {
        _sequenceManager = sequenceManager;
        _timerManager = timerManager;
        _logging = logging;
        _cfgGas = gasConfig;
        _cfgPlasma = plsConfig;
        _cfgVacuum = vacuumConfig;
        _cfgVentilation = ventilationConfig;
        _cfgLeakCheck = leakCheckCfg;
        _cim = cim;
        _pump = pump;
        _booster = booster;
        _n2 = purge;
        _gaugeValve = gaugeValve;
        _recipe = recipe;
        _chamber = chamber;
        _vacValve = vacuumValve;
        _vacGauge = vacGauge;
        _rf = generator;
        _gas = gas;

        _door = autoDoor;
        _cooler = cool;
        _jobSink = jobEnd;

        InitTimer();
        InitModulePlasma();
    }

    private void InitModulePlasma()
    {
        var plasmaModules = new List<ISequenceModule>();
        plasmaModules.Add(new PlasmaSequence(_logging, _timerManager, _cfgPlasma, _recipe, _cim, _rf, _vacGauge, _gas, _vacValve, _chamber, PlasmaCountForOneHour, PlasmaActionProcess));
        plasmaModules.Add(new VacuumPumpingSequence(_logging, _timerManager, _cfgVacuum, _chamber, _pump, _booster, _vacValve, _gaugeValve, _vacGauge, _cim, _recipe, PlasmaActionProcess));
        plasmaModules.Add(new GasPurgingSequence(_logging, _timerManager, _cfgGas, _gas, _vacValve, _vacGauge, _chamber, PlasmaActionProcess));
        plasmaModules.Add(new VentilationSequence(_logging, _timerManager, _cfgVentilation, _cim, _pump, _booster, _n2, _gaugeValve, _chamber, PlasmaActionProcess, _jobSink, _door, _cooler));
        plasmaModules.Add(new LeakCheckSequence(_logging, _timerManager, _cfgLeakCheck, _recipe, _chamber, _pump, _booster, _vacValve, _gaugeValve, _vacGauge, _n2, LeakCheckDataAction, LeakInfo));
        _sequenceManager.AddModule(plasmaModules);
        _sequenceManager.AlwayRunAction += AlwaysOnTimer;
    }

    private void InitTimer()
    {
        _timerManager.Add((int)eTimer.GasTimer);
        _timerManager.Add((int)eTimer.GasStableTimer);
        _timerManager.Add((int)eTimer.VacStableTimer);
        _timerManager.Add((int)eTimer.GasErrOnProcessTimer);
        _timerManager.Add((int)eTimer.PwrCheckDelayTimer);
        _timerManager.Add((int)eTimer.RfOnTimer);
        _timerManager.Add((int)eTimer.StepTimer);
        _timerManager.Add((int)eTimer.VacErrOnProcessTimer);
        _timerManager.Add((int)eTimer.RfErrOnProcessTimer);
        _timerManager.Add((int)eTimer.SoftPowerTimer);
        _timerManager.Add((int)eTimer.VacTimer);    
        _timerManager.Add((int)eTimer.PurgeTimer);
        _timerManager.Add((int)eTimer.VentilationTimer);
        _timerManager.Add((int)eTimer.PumpTimer);
        _timerManager.Add((int)eTimer.LeakCheckTimer);
        _timerManager.Add((int)eTimer.ElapsedTimer);
    }

    private void AlwaysOnTimer()
    {
        PlasmaParameterAction.Step?.Invoke(PlasmaDevice.Instance.GetCurrentStep());
        PlasmaParameterAction.RFGenerator?.Invoke((int)_rf.GetForwardWatt(), (int)_rf.GetReflectedWatt());
        PlasmaParameterAction.Vacuum?.Invoke(_vacGauge.Torr());
        PlasmaParameterAction.Gas?.Invoke(_gas.ReadGas(0), _gas.ReadGas(1), _gas.ReadGas(2), _gas.ReadGas(3));
        PlasmaParameterAction.CleaningTimeMs?.Invoke((int)_timerManager.GetOrAdd((int)eTimer.StepTimer).TotalMilliseconds);
        PlasmaParameterAction.GeneratorRunMs?.Invoke((int)_timerManager.GetOrAdd((int)eTimer.RfOnTimer).TotalMilliseconds);
        PlasmaParameterAction.PumpRunMs?.Invoke((int)_timerManager.GetOrAdd((int)eTimer.PumpTimer).TotalMilliseconds);
        PlasmaParameterAction.PurgeTimeMs?.Invoke((int)_timerManager.GetOrAdd((int)eTimer.PurgeTimer).TotalMilliseconds);
        PlasmaParameterAction.ValveStatus?.Invoke();
    }

    public void PlasmaStart()
    {
        PlasmaDevice.Instance.PlasmaStart();
    }

    public void PlasmaStop()
    {
        PlasmaDevice.Instance.PlasmaStart(false);
    }

    public void LeakCheckStart()
    {
        PlasmaDevice.Instance.LeakCheckStart();
    }

    public void LeakCheckStop()
    {
        PlasmaDevice.Instance.LeakCheckStart();
    }

    public void Disposable()
    {

    }
}
