using System.Diagnostics;

namespace VsFoundation.Sequence.Bases;

public class UnitStep
{
    public Func<bool> CheckCondition { get; set; } = () => true;
    public Func<bool> ExecuteAction { get; set; } = () => true;
    public Action ActionTimeout { get; set; }
    public Func<bool> IsStateOK { get; set; } = () => true;
    public Func<int> NextStep { get; set; }

    private int _timeout { get; set; } = 0;
    private bool _conditionFalse = true;
    private Stopwatch _time = new();
    private bool _hasExecuted = false;
    private bool _isExecuteCompleted = false;
    private bool _canceled = false;


    public UnitStep(int timeout = 0)
    {
        _timeout = timeout;
        _time = new Stopwatch();
    }

    public void Start()
    {
        _canceled = false;

        if (!(CheckCondition?.Invoke() ?? true))
        {
            _conditionFalse = true;
            return;
        }

        _conditionFalse = false;

        if (!_hasExecuted)
        {
            SetTimer();
            _hasExecuted = true;
        }

        if ((ExecuteAction?.Invoke() ?? true) && _hasExecuted)
        {
            _isExecuteCompleted = true;
            IsTimeout();
        }    
    }

    public bool IsComplete()
    {
        if (_canceled) return false;
        if (_conditionFalse) return false;
        if (!_isExecuteCompleted) return false;
        var result = (IsStateOK?.Invoke() ?? true);
        if (result)
        {
            //_hasExecuted = false;
            //_isExecuteCompleted = false;
            ResetState();
        }    
        return result;
    }

    public bool IsTimeout()
    {
        if (_canceled) return false;
        if (_conditionFalse) return false;
        if (!_isExecuteCompleted) return false;
        if (_timeout <= 0) return false;

        if (_time.ElapsedMilliseconds > _timeout)
        {
            //_hasExecuted = false;
            //_isExecuteCompleted = false;
            ResetState();
            ActionTimeout?.Invoke();

            return true;
        }

        return false;
    }

    public void Cancel()
    {
        _canceled = true;
        ResetState();
    }

    private void ResetState()
    {
        _hasExecuted = false;
        _isExecuteCompleted = false;
        _conditionFalse = true;
        _time.Reset();
    }

    private void SetTimer()
    {
        _time.Restart();
    }
}
