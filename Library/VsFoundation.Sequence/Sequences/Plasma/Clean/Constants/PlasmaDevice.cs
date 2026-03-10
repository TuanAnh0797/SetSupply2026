using System.Diagnostics;

namespace VsFoundation.Sequence.Sequences.Plasma.Clean.Constants;

public class PlasmaDevice
{
    private static readonly Lazy<PlasmaDevice> _instance = new Lazy<PlasmaDevice>(() => new PlasmaDevice());

    public static PlasmaDevice Instance => _instance.Value;

    protected struct MGZ
    {
        public readonly int _PM;
        public int _nCleanState;
        public Dictionary<int, string> _cleanState;
    }

    private PlasmaDevice() { Init(); }

    private bool _gasReady = default;
    private int _currentStep = default;
    private bool _gasError = default;
    private bool _vacTimeOver = default;
    private bool _plasmaSkip = default;
    private bool _softStartFlag = default;
    private bool _softEndFlag = default;
    private bool _ventilationAutoDoorFlag = default;
    private bool _complete = default;
    private bool _unload = default;
    private bool[] _process;
    private MGZ _mgz;
    private int _max_Process = Enum.GetValues(typeof(eProcess)).Length;
    private bool _startPlasma = default;
    private bool _startLeakCheck = default;

    public void Clear()
    {
        ResetGasReady();
        ResetCurrentStep();
        ResetGasError();
        ResetVacTimeOver();
        SetFlagVentilationAutoDoor(false);
    }
    public void Init()
    {
        _process = new bool[_max_Process];
        _mgz._cleanState = new Dictionary<int, string>();

        _mgz._cleanState.Add((int)eCleaningState.PM_READY, "PM_READY");
        _mgz._cleanState.Add((int)eCleaningState.PM_CLEANING, "PM_CLEANING");
        _mgz._cleanState.Add((int)eCleaningState.PM_CANCELING, "PM_CANCELING");
        _mgz._cleanState.Add((int)eCleaningState.PM_COMPLETE, "PM_COMPLETE");

        SetCleanState((int)eCleaningState.PM_READY);
    }

    public int GetCleanState()
    {
        return _mgz._nCleanState;
    }

    public void SetCleanState(int nState)
    {
        if (_mgz._nCleanState != nState)
        {
            _mgz._nCleanState = nState;
        }
    }

    public void Set_Process(eProcess process, bool set = true)
    {
        _process[(int)process] = set;
    }

    public bool Get_Process(eProcess process)
    {
        return _process[(int)process];
    }

    public bool IsGasReady()
    {
        return _gasReady;
    }

    public void SetGasReady()
    {
        _gasReady = true;
    }

    public void ResetGasReady()
    {
        _gasReady = false;
    }

    public void IncreaseStep()
    {
        _currentStep++;
    }

    public int GetCurrentStep()
    {
        return _currentStep;
    }

    public void ResetCurrentStep()
    {
        _currentStep = 0;
    }

    public bool IsGasError()
    {
        return _gasError;
    }

    public void SetGasError()
    {
        _gasError = true;
    }

    public void ResetGasError()
    {
        _gasError = false;
    }

    public void SetVacTimeOver()
    {
        _vacTimeOver = true;
    }

    public void ResetVacTimeOver()
    {
        _vacTimeOver = false;
    }

    public bool IsVacTimeOver()
    {
        return _vacTimeOver;
    }

    public void SetFlagVentilationAutoDoor(bool status)
    {
        _ventilationAutoDoorFlag = status;
    }

    public bool GetFlagVentilationAutoDoor()
    {
        return _ventilationAutoDoorFlag;
    }

    public void SetUnload()
    {
        _unload = true;
    }

    public void ResetUnload()
    {
        _unload = false;
    }

    public bool GetUnload()
    {
        return _unload;
    }

    public void PlasmaStart(bool status = true)
    {
        _startPlasma = status;
    }

    public bool IsPlasmaStart()
    {
        return _startPlasma;
    }

    public void LeakCheckStart(bool status = true)
    {
        _startLeakCheck = status;
    }

    public bool IsLeakCheck()
    {
        return _startLeakCheck;
    }
}
