using VsFoundation.Sequence.Manager;
using VsFoundation.Sequence.Restore;
using System.Diagnostics;
using VsFoundation.Base.DI.Sequence;
using VsFoundation.Base.Constants.Sequence;

namespace VsFoundation.Sequence.Bases;

public abstract class BaseSequence : ISequenceModule
{
    protected int _currentStep;
    protected Stopwatch _time;
    protected bool _work;
    protected UnitStep _unitStep;
    private readonly SequenceHistoryManager _history;
    protected Dictionary<int, UnitStep> actionUnitStep = new Dictionary<int, UnitStep>();
    protected int _homeStep = 9000;
    protected double _initProgressPercent;

    public abstract int ModuleId { get; set; }
    public virtual string ModuleName { get; } = string.Empty;
    public virtual string LogHead { get; } = string.Empty;
    public bool IsInitialized { get; set; }
    public Action<eSeqState> Action { get; set; }
    public Action<int, Func<int, string>> StepChanged { get; set; }
    public Action<int> AlarmTriggered { get; set; }
    public eOptionRun OptionRun { get; set; } = eOptionRun.AutoRun;
    public Action<int> ProgessInitialize { get; set; }
    public bool EnableModule { get; set; }
    public virtual bool CanInitialize { get; } = false;
    public virtual bool CanMonitoring { get; } = true;
    public eRunMode RunMode { get; set; }
    public bool IsReady { get; set; }
    public bool IsCompleted { get; set; }

    public BaseSequence()
    {
        _currentStep = -1;
        _time = new Stopwatch();
        _work = false;
        _history = new SequenceHistoryManager();
    }

    public abstract eSequenceResult RunSequence();
    public virtual void AlwaysRun(eSeqState state) { StepChanged?.Invoke(_currentStep, GetStepName); }
    public virtual void ClearAlarm() { if (!EnableModule) return; }
    protected virtual void Savestore(SequenceRestore state) { if (!EnableModule) return; }
    protected virtual bool Restore(SequenceRestore state) { if (!EnableModule) return true; return false; }

    public virtual bool Initialize()
    {
        if (!EnableModule) return true;

        IsInitialized = true;

        return IsInitialized;
    }

    public virtual void FirstHomeCmd()
    {
        IsInitialized = false;
        _currentStep = _homeStep;
    }

    public virtual bool IsReadySeq()
    {
        return !GetState();
    }

    public virtual void Start()
    {
        if (!EnableModule) return;

        if (!RestoreState(_history.Undo()!)) return;

        if (_currentStep < 0)
            _currentStep = 0;
        StepChanged?.Invoke(_currentStep, GetStepName);

        _work = true;
        FlagManager.SetFlag(ModuleId, true);
    }

    public virtual void Stop()
    {
        if (!EnableModule) return;

        _work = false;
        FlagManager.SetFlag(ModuleId, false);

        foreach(var unit in actionUnitStep)
        {
            unit.Value.Cancel();
        }

        var restore = SaveState();
        _history.Save(restore);
    }

    public void Cancel()
    {
        _work = false;
        _currentStep = -1;
        StepChanged?.Invoke(_currentStep, GetStepName);
        _history.Clear();
        FlagManager.SetFlag(ModuleId, false);
    }

    public void SetStep(int step)
    {
        _currentStep = step;
    }

    public bool GetWork()
    {
        return _work;
    }

    public bool GetState()
    {
        return FlagManager.IsRunning(ModuleId);
    }

    protected void GetProgress(Type enumType, int current)
    {
        var steps = Enum.GetValues(enumType).Cast<int>().ToList();
        int total = steps.Count - 1;
        if (current == steps.Max())
        {
            ProgessInitialize?.Invoke(100);
            return;
        }    

        int index = steps.IndexOf(current);
        if (index < 0)
        {
            ProgessInitialize?.Invoke(0);
            return;
        }    

        var progess = (index * 100) / total;
        ProgessInitialize?.Invoke(progess);
    }

    protected void GetProgress(int progess)
    {
        ProgessInitialize?.Invoke(progess);
    }

    protected UnitStep GetUnitStep(int step = 0)
    {
        int stepId = _currentStep;

        if (step != 0)
            stepId = step;

        if(actionUnitStep.TryGetValue(stepId, out UnitStep unit))
        {
            unit.Start();

            return unit;
        }    

        return null;
    }

    protected void NextStep(int step)
    {
        SetTimer();

        if (step == -1)
            _currentStep++;
        else
            _currentStep = step;
        StepChanged?.Invoke(_currentStep, GetStepName);
    }

    protected void PreStep()
    {
        if (_currentStep < 0)
            _currentStep = -1;
        else
            _currentStep--;
        StepChanged?.Invoke(_currentStep, GetStepName);
    }

    protected virtual void SetAlarm(int errorCode)
    {
        Stop();
        Action?.Invoke(eSeqState.ERROR);
        AlarmTriggered?.Invoke(errorCode);
    }

    protected bool Timeout(int milliseconds)
    {
        if (milliseconds <= 0) return false;

        return Delay(milliseconds);
    }

    protected bool Delay(int milliseconds)
    {
        return _time.ElapsedMilliseconds > milliseconds;
    }

    private void SetTimer()
    {
        _time.Restart();
    }

    private SequenceRestore SaveState()
    {
        SequenceRestore state = new SequenceRestore
        {
            Step = _currentStep,
        };

        Savestore(state);

        return state;
    }

    public bool RestoreState(SequenceRestore state)
    {
        if (state == null) return false;

        _currentStep = state.Step;
        StepChanged?.Invoke(_currentStep, GetStepName);

        return Restore(state);
    }

    public int GetStep()
    {
        return _currentStep;
    }

    public virtual string GetStepName(int stepInx) => stepInx.ToString();
}
