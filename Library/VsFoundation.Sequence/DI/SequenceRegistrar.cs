using Microsoft.Extensions.DependencyInjection;
using VsAlarmConfig;
using VsFoundation.Base.Constants.Sequence;
using VsFoundation.Base.Constants.TowerLamp;
using VsFoundation.Base.DI.Sequence;
using VsFoundation.Base.Logger.Interfaces;
using VsFoundation.Base.Models.TowerLamp;
using VsFoundation.Sequence.Bases.TowerLamp;
using VsFoundation.Sequence.Constants.IO.Monitoring;
using VsFoundation.Sequence.Manager;
using VsFoundation.Sequence.Sequences.Plasma.Clean;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Configs;
using VsFoundation.Sequence.Sequences.Plasma.Clean.Interfaces;
using VSLibrary.Common.MVVM.Core;
using VSLibrary.Common.MVVM.Models;

namespace VsFoundation.Sequence.DI;

public static class SequenceRegistrar
{
    public static void RegisterTowerLamp(TowerSettings setting, dynamic doGreen, dynamic doRed, dynamic doYellow, dynamic buzzer, Dictionary<eTowerLamp, IDiscreteOutput> outputLamps = null)
    {
        var outputs = new Dictionary<eTowerLamp, IDiscreteOutput>();

        if (outputLamps == null)
        {
            // Map TowerLamp to actual output controls
            outputs = new Dictionary<eTowerLamp, IDiscreteOutput>
                {
                    { eTowerLamp.Green , new DioAdapter("Green" , doGreen) },
                    { eTowerLamp.Red   , new DioAdapter("Red"   , doRed) },
                    { eTowerLamp.Yellow, new DioAdapter("Yellow", doYellow) },
                    { eTowerLamp.Buzzer, new DioAdapter("Buzzer", buzzer) },
                };
        }
        else
        {
            outputs = outputLamps;
        }

        var tower = new TowerLampManager(outputs, null);
        tower.SetMode(eTowerMode.Stopped);
        VSContainer.Instance.RegisterInstance<ITowerLampManager>(tower);
    }

    public static void RegisterTimerManager()
    {
        TimerManager timerManager = new TimerManager();
        VSContainer.Instance.RegisterInstance<ITimerManager>(timerManager);
    }

    public static ISequenceManager RegisterSequence(List<ISequenceModule> sequenceModules = null)
    {
        // Register Sequence
        SequenceManager sequenceManager = new SequenceManager();
        VSContainer.Instance.RegisterInstance<ISequenceManager>(sequenceManager);

        sequenceManager.AddModule(sequenceModules);

        //Register Alarm action for TowerLamp
        var towerLamp = VSContainer.Instance.Resolve<ITowerLampManager>();
        sequenceManager.StateChanged += (state) =>
        {
            if (state == eSeqState.RUNNING)
            {
                towerLamp?.SetMode(eTowerMode.Run);
            }
            else if (state != eSeqState.ERROR && state != eSeqState.RUNNING)
            {
                towerLamp?.SetMode(eTowerMode.Stopped);
            }
            else if (state == eSeqState.ERROR)
            {
                towerLamp?.SetMode(eTowerMode.Error);
            }
        };

        return sequenceManager;
    }

    public static void RegisterSeqPlasma(List<ISequenceModule> sequenceModules, GasPurgingConfig gasConfig, 
        PlasmaConfig plsConfig, VacuumConfig vacuumConfig, VentilationConfig ventilationConfig, LeakCheckConfig leakCheckCfg,
        ICimReporter cim, IDryPump pump, IBoosterPump booster,
        IN2Purge purge, IGaugeValve gaugeValve, IRecipeProvider recipe, IChamber chamber,
        IVacuumValve vacuumValve, IVacuumGauge vacGauge, IRFGenerator generator, IGas gas, IJobCompleteSink jobEnd = null,
        IAutoDoor autoDoor = null, IAutoCooler cool = null)
    {
        RegisterSequence(sequenceModules);
        var sequenceManager = VSContainer.Instance.Resolve<ISequenceManager>();
        var timerManager = VSContainer.Instance.Resolve<ITimerManager>();
        var logger = VSContainer.Instance.MS_Services.GetRequiredService<ILoggingService>();
        logger.Prefix = "Sequence";

        ISeqCleaning seqCleaning = new SeqCleaning(sequenceManager, timerManager, logger,
            gasConfig, plsConfig,
            vacuumConfig, ventilationConfig, leakCheckCfg,
            cim, pump,
            booster, purge, gaugeValve,
            recipe, chamber, vacuumValve,
            vacGauge, generator, gas,
            jobEnd);

        VSContainer.Instance.RegisterInstance<ISeqCleaning>(seqCleaning);
    }

    public static void RegisterAlarm(List<ErrorItem> errors)
    {
        var alarmManager = new AlarmManager(errors);
        VSContainer.Instance.RegisterInstance<AlarmManager>(alarmManager);
        var sequenceManager = VSContainer.Instance.Resolve<ISequenceManager>();
        alarmManager.MachineStopWhenAlarm += () =>
        {
            sequenceManager.Stop();
        };
        sequenceManager.AlarmTriggered += alarmManager.AlarmOfSequence;
    }

    public static void RegisterIOMonitoring<TDI, TDO, TAI, TAO>()
        where TDI : struct, Enum
        where TDO : struct, Enum
        where TAI : struct, Enum
        where TAO : struct, Enum
    {
        VSContainer.Instance.RegisterInstance<Type>(typeof(TDO));

        var genericMonitor = new ControllerMonitoring<TDI, TDO, TAI, TAO>();

        VSContainer.Instance.RegisterInstance<IControllerMonitoring<TDI, TDO, TAI, TAO>>(genericMonitor);
        VSContainer.Instance.RegisterInstance<IControllerMonitoring>(
            new ControllerMonitoringAdapter<TDI, TDO, TAI, TAO>(genericMonitor));
    }
}
