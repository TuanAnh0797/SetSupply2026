using VsFoundation.Base.Constants.Sequence;
using VsFoundation.Base.DI.Sequence;
using VsFoundation.Sequence.Constants;

namespace VsFoundation.Sequence.Manager;

/// <summary>
/// Manages the execution of all sequence modules, including initialization,
/// start/stop control, state management, and alarm handling.
/// </summary>
/// <remarks>
/// This class acts as the central controller for coordinating multiple 
/// <see cref="ISequenceModule"/> instances. It provides:
/// <list type="bullet">
/// <item><description>Initialization and home process handling.</description></item>
/// <item><description>Run modes control (Auto, Step, Cycle).</description></item>
/// <item><description>Global state management (READY, RUNNING, PAUSE, ERROR, STOP).</description></item>
/// <item><description>Alarm propagation and clearing across modules.</description></item>
/// <item><description>Main loop thread to monitor and update sequence execution.</description></item>
/// </list>
/// </remarks>
public class SequenceManager : ISequenceManager
{
    private readonly List<ISequenceModule> _modules = new();
    private eRunMode _mode;
    private eOptionRun _optionRun;
    private bool _isRunning = true;
    private bool _manualTrigger = false;
    private bool _firstHomeFlag = false;
    private Thread _thread;
    private eSeqState _stateSequence = eSeqState.STOP;
    private eSeqState _prevState = eSeqState.STOP;
    private ISequenceModule? _currentManualModule;
    private eModeRunSequence _modeRun = eModeRunSequence.Synchronous;

    public int LoopTime { get; private set; } = 10;

    /// <summary>
    /// Occurs when the sequence state changes. 
    /// Provides the new <see cref="eSeqState"/> as a parameter.
    /// </summary>
    public Action<eSeqState> StateChanged { get; set; }

    /// <summary>
    /// Event triggered when an alarm occurs in any module. 
    /// The parameter is the alarm code or ID.
    /// </summary>
    public event Action<int>? AlarmTriggered;

    /// <summary>
    /// An action that is always executed in the main loop,
    /// regardless of the current sequence state.
    /// </summary>
    public Action AlwayRunAction { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="SequenceManager"/> 
    /// and sets how the sequence modules will be executed.
    /// </summary>
    /// <param name="mode">
    /// Defines how the sequence will run:
    /// <list type="bullet">
    /// <item><description><see cref="eModeRunSequence.Synchronous"/> – Executes modules sequentially, one after another.</description></item>
    /// <item><description><see cref="eModeRunSequence.Parallel"/> – Executes modules in parallel, allowing simultaneous operation.</description></item>
    /// </list>
    /// Defaults to <see cref="eModeRunSequence.Synchronous"/> if not specified.
    /// </param>
    public SequenceManager(eModeRunSequence mode = eModeRunSequence.Synchronous)
    {
        _modeRun = mode;
    }

    /// <summary>
    /// Initializes and starts the main sequence loop in a background thread.
    /// </summary>
    public void Init()
    {
        _thread = new Thread(MainLoop);
        _thread.IsBackground = true;
        _thread.Start();
    }

    /// <summary>
    /// Sets the option run mode for the entire sequence.
    /// </summary>
    /// <param name="optionRun">
    /// The option run mode to apply (DryRun, AutoRun).
    /// </param>
    /// <returns>
    /// True if the option was successfully applied; 
    /// False if the sequence is currently running (no change applied).
    /// </returns>
    /// <remarks>
    /// - This method first checks if the sequence state is RUNNING.  
    ///   If it is running, the option cannot be changed and the method returns false.  
    /// - If not running, it updates the internal <see cref="_optionRun"/> field 
    ///   and propagates the new option to all registered modules in <see cref="_modules"/>.
    /// </remarks>
    public bool SetOptionRun(eOptionRun optionRun)
    {
        if (_stateSequence == eSeqState.RUNNING) return false;
        _optionRun = optionRun;
        _modules.ForEach(x => x.OptionRun = _optionRun);
        
        return true;
    }

    /// <summary>
    /// Gets the previous state of the sequence before the current state change.
    /// </summary>
    /// <returns>
    /// The previous <see cref="eSeqState"/> value that was stored before the last state update.
    /// </returns>
    public eSeqState GetPreviousState()
    {
        return _prevState;
    }

    /// <summary>
    /// Gets the current state of the sequence.
    /// </summary>
    /// <returns>The current <see cref="eSeqState"/> value.</returns>
    public eSeqState GetState()
    {
        return _stateSequence;
    }

    /// <summary>
    /// Adds a list of sequence modules to the manager
    /// </summary>
    /// <param name="modules">
    /// The list of <see cref="ISequenceModule"/> instances to be added.
    /// </param>
    public void AddModule(List<ISequenceModule> modules)
    {
        if (modules == null) return;

        _modules.AddRange(modules);

        foreach (var module in modules)
        {
            module.Action += (status) => {
                SetState(status);
            };
            module.AlarmTriggered += OnModuleAlarmTriggered;
        }    
    }

    /// <summary>
    /// Gets the sequence module at the specified index.
    /// </summary>
    /// <param name="moduleId">
    /// The zero-based index of the module in the internal module list.
    /// </param>
    /// <returns>
    /// The <see cref="ISequenceModule"/> at the given index.
    /// </returns>
    public ISequenceModule GetModule(int moduleId)
    {
        return _modules[moduleId];
    }

    /// <summary>
    /// Gets the list of all registered sequence modules.
    /// </summary>
    /// <returns>
    /// A <see cref="List{ISequenceModule}"/> containing all modules managed by this instance.
    /// </returns>
    public List<ISequenceModule> GetAllModules()
    {
        return _modules;
    }

    /// <summary>
    /// Performs the "home" initialization process for the sequence.
    /// </summary>
    /// <param name="enableInitializes">
    /// Optional list of module IDs to enable for initialization.  
    /// If null or empty, all modules will be enabled.
    /// </param>
    public void Initial(List<int> enableInitializes = null)
    {
        if (_stateSequence == eSeqState.RUNNING || 
            _stateSequence == eSeqState.ERROR) return;

        EnableModule(enableInitializes);

        Cancel();

        SetState(eSeqState.INITIALIZE);
    }

    /// <summary>
    /// Clears alarms for all modules and restores the previous sequence state.
    /// </summary>
    /// <remarks>
    /// If the sequence is currently RUNNING, the method does nothing.
    /// </remarks>
    public void ClearAlarm()
    {
        if (_stateSequence == eSeqState.RUNNING) return;
        _modules.ForEach(x => x.ClearAlarm());

        SetState(_prevState);
    }

    /// <summary>
    /// Checks whether all modules in the sequence have been initialized.
    /// </summary>
    /// <returns>
    /// True if every module's <see cref="ISequenceModule.IsInitialized"/> property is true; otherwise, false.
    /// </returns>
    public bool SequenceHasBeenInitialized()
    {
        return _modules.All(x => x.IsInitialized);
    }

    /// <summary>
    /// Starts the sequence by enabling the specified modules, 
    /// setting the run mode to Auto, and transitioning to the RUNNING state.
    /// </summary>
    /// <param name="moduleStart">
    /// Optional list of module IDs to start. If null, all modules will be enabled and started.
    /// </param>
    /// <returns>
    /// True if the sequence was successfully started; false if the current state is not READY or PAUSE.
    /// </returns>
    public bool Start(List<int> moduleStart = null)
    {
        if (_stateSequence != eSeqState.READY && _stateSequence != eSeqState.PAUSE)
            return false;

        EnableModule(moduleStart);

        SetMode(eRunMode.Auto);
        _firstHomeFlag = false;

        _modules.ForEach(x => x.Start());
        SetState(eSeqState.RUNNING);
        return true;
    }

    /// <summary>
    /// Stops the sequence and updates the state to either ERROR or PAUSE.
    /// </summary>
    /// <param name="isStopWhenError">
    /// If true, sets the sequence state to ERROR; otherwise, sets it to PAUSE.
    /// </param>
    public void Stop(bool isStopWhenError = false)
    {
        SetSequenceStop();
        if (isStopWhenError)
            SetState(eSeqState.ERROR);
        else
            SetState(eSeqState.PAUSE);
    }

    /// <summary>
    /// Cancels the current operation of all modules and sets the sequence state to PAUSE.
    /// </summary>
    /// <remarks>
    /// Does nothing if the sequence is currently RUNNING or in INITIALIZE state.
    /// Resets the first-home flag before cancelling modules.
    /// </remarks>
    public void Cancel()
    {
        if (_stateSequence == eSeqState.RUNNING || _stateSequence == eSeqState.INITIALIZE) return;
        _firstHomeFlag = false;

        _modules.ForEach(x => x.Cancel());
        SetState(eSeqState.PAUSE);
    }

    /// <summary>
    /// Disposes the sequence manager by stopping the sequence, 
    /// canceling all operations, and waiting for the main thread to complete.
    /// </summary>
    public void Disposable()
    {
        SetState(eSeqState.STOP);
        _isRunning = false;
        Cancel();
        _thread.Join();
    }

    /// <summary>
    /// Runs the specified module in the given run mode. 
    /// Supports Auto, Step, and Cycle execution modes.
    /// </summary>
    /// <param name="mode">
    /// The <see cref="eRunMode"/> to execute (Auto, Step, Cycle, etc.).
    /// </param>
    /// <param name="moduleId">
    /// The ID of the module to run (used for Step or Cycle modes).
    /// </param>
    /// <param name="manualStepIndex">
    /// Optional manual step index to execute when running in Step or Cycle mode.
    /// </param>
    public void Run(eRunMode mode, int moduleId, int? manualStepIndex = null)
    {
        SetMode(mode);

        if (_mode == eRunMode.Auto || _stateSequence == eSeqState.RUNNING) return;

        if ((_mode == eRunMode.Step || _mode == eRunMode.Cycle) && manualStepIndex.HasValue)
        {
            _currentManualModule = _modules.FirstOrDefault(c => c.ModuleId == moduleId);

            if (_currentManualModule == null) return;

            _currentManualModule.SetStep(manualStepIndex.Value);
            _manualTrigger = true;
        }
    }

    #region Internal function
    private void SetState(eSeqState state)
    {
        if (_stateSequence == state)
            return;

        _prevState = _stateSequence;
        _stateSequence = state;

        if (state == eSeqState.ERROR)
        {
            SetSequenceStop();
        }

        StateChanged?.Invoke(state);
    }

    private void SetMode(eRunMode mode)
    {
        if (_modules == null) return;
        _mode = mode;
        _modules.ForEach(x => x.RunMode = mode);
    }

    private void EnableModule(List<int> modules)
    {
        _modules.ForEach(x => x.EnableModule = false);

        if (modules == null || modules.Count <= 0)
        {
            _modules.ForEach(x => x.EnableModule = true);
        }
        else
        {
            for (int i = 0; i < modules.Count; i++)
            {
                var module = _modules.FirstOrDefault(x => x.ModuleId == modules[i]);

                if (module == null) continue;

                module.EnableModule = true;
            }
        }
    }

    private void OnModuleAlarmTriggered(int alarmCode)
    {
        AlarmTriggered?.Invoke(alarmCode);
    }

    private void SetSequenceStop()
    {
        SetMode(eRunMode.Cycle);
        _firstHomeFlag = false;

        _modules.ForEach(x => x.Stop());
    }

    private void MainLoop()
    {
        while (_isRunning)
        {
            Thread.Sleep(LoopTime);
            ManualRun();
            AlwayRun();
            Initialize();
            AutoRun();
        }
    }

    private void ManualRun()
    {
        if (_mode != eRunMode.Step && _mode != eRunMode.Cycle)
            return;

        if (_manualTrigger && _currentManualModule != null)
        {
            var result = _currentManualModule.RunSequence();

            if (result == eSequenceResult.SUCCESS || result == eSequenceResult.FAILD)
            {
                _manualTrigger = false;
            }
        }
    }

    private void AutoRun()
    {
        if (_stateSequence != eSeqState.RUNNING)
            return;

        if (_mode != eRunMode.Auto)
            return;

        RunModules(module => 
        {
            if (!module.EnableModule) return;
            module.RunSequence();
        });
    }

    private void Initialize()
    {
        if (_stateSequence != eSeqState.INITIALIZE) return;

        if (_modules.All(m => m.IsInitialized))
        {
            _modules.ForEach(x => x.EnableModule = false);
            _firstHomeFlag = false;
            SetState(eSeqState.READY);
            return;
        }

        if (_modeRun == eModeRunSequence.Synchronous)
        {
            foreach (var module in _modules)
            {
                if (!module.EnableModule) continue;
                if(!_firstHomeFlag)
                    module.FirstHomeCmd();

                module.Initialize();
            }

            _firstHomeFlag = true;
        }
        else if(_modeRun == eModeRunSequence.Parallel)
        {
            Parallel.ForEach(_modules, module =>
            {
                if (!module.IsInitialized && module.EnableModule)
                {
                    if (!_firstHomeFlag)
                        module.FirstHomeCmd();

                    module.Initialize();
                }

                _firstHomeFlag = true;
            });
        }    
    }

    private void AlwayRun()
    {
        AlwayRunAction?.Invoke();
        RunModules(module => module.AlwaysRun(_stateSequence));
    }

    private void RunModules(Action<ISequenceModule> action)
    {
        if (_modeRun == eModeRunSequence.Synchronous)
        {
            foreach (var module in _modules)
                action(module);
        }
        else if (_modeRun == eModeRunSequence.Parallel)
        {
            Parallel.ForEach(_modules, action);
        }
    }
    #endregion
}
