using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsFoundation.Base.Constants.Motion;
using VsFoundation.Base.Constants.Sequence;
using VsFoundation.Base.Logger.Interfaces;
using VsFoundation.Sequence.Bases;
using VsFoundation.Sequence.Constants;
using VsFoundation.Sequence.Manager;
using VsFoundation.Sequence.Restore;
using VsFoundation.Sequence.Sequences.Plasma.LoadPusher.Configs;
using VsFoundation.Sequence.Sequences.Plasma.LoadPusher.Interfaces;

namespace VsFoundation.Sequence.Sequences.Plasma.LoadPusher;

public class LoadPusherSequence : BaseSequence
{
    public override int ModuleId { get; set; } = -1; //TODO: update later
    public override string ModuleName => "LOADING PUSHER X";
    public override string LogHead => "LOAD_PUSHER_X";
    public override bool CanInitialize => true;

    private readonly LoadPusherConfig _config;
    private readonly ILoggingService _logger;
    private readonly ILoadPusherCondition _pusherConditions;
    private readonly ILoadPusherBase _pusherBase;

    private Action<bool> _pusherActionReady;
    private Action<bool> _pusherActionConmplete;
    public LoadPusherSequence(LoadPusherConfig config, ILoggingService logger, ILoadPusherBase pusherBase, ILoadPusherCondition pusherConditions, Action<bool> pusherActionReady, Action<bool> pusherActionConmplete)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pusherBase = pusherBase ?? throw new ArgumentNullException(nameof(pusherBase));
        _pusherConditions = pusherConditions ?? throw new ArgumentNullException(nameof(pusherConditions));
        _pusherActionReady += pusherActionReady;
        _pusherActionConmplete += pusherActionConmplete;
        InitStep();

    }

    protected override bool Restore(SequenceRestore state)
    {
        return true;
    }

    protected override void Savestore(SequenceRestore state)
    {

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

    public override void ClearAlarm()
    {
        base.ClearAlarm();
    }

    public override void FirstHomeCmd()
    {
        base.FirstHomeCmd();
        _currentStep = (int)eStepInit.START;
        GetProgress(0);
    }

    public override void AlwaysRun(eSeqState state)
    {
        base.AlwaysRun(state);

        
    }

    public override bool Initialize()
    {
        if (IsInitialized) return true;
        _unitStep = GetUnitStep();

        UnitStepCheck();

        GetProgress(typeof(eStepInit), _currentStep);

        switch ((eStepInit)_currentStep)
        {
            case eStepInit.START:
            case eStepInit.STEP_10_SET_INITIALIZE:
            case eStepInit.Step_11_Servo_X_Home:
            case eStepInit.Step_12_Servo_X_Ready:
                break;

            case eStepInit.END:
                IsInitialized = true;
                break;
        }
        return IsInitialized;
    }

    protected override void SetAlarm(int nErrorCode)
    {
        _logger.LogError(string.Format("{0}: Error No.{1} Step {2}", LogHead, nErrorCode, _currentStep));
        base.SetAlarm(nErrorCode);
    }

    public override string GetStepName(int step) => ((eStep)step).ToString();
    private void NextStep(int step = -1)
    {
        if (!GetWork()) return;
        base.NextStep(step);
        _logger.LogInfo(string.Format("{0}: Sequence Step {1}", LogHead, (eStep)_currentStep));
    }
    private void UnitStepCheck()
    {
        if (_unitStep == null) return;
        if (!_unitStep.IsComplete()) return;

        NextStep(_unitStep.NextStep());
    }
    public override eSequenceResult RunSequence()
    {
        if (!GetWork()) return eSequenceResult.NOT_READY;

        switch ((eStep)_currentStep)
        {
            case eStep.START:
            case eStep.STEP_1_CHECK_READY:
            case eStep.Step_2_Check_Servo_X_Ready:
            case eStep.STEP_5_DIVERGENCE:
            case eStep.STEP_10_PUSHER_EXTEND:
            case eStep.Step_11_Servo_X_Ext_Start:
            case eStep.Step_12_Servo_X_Ext_End:
                if (_currentStep == (int)eStep.Step_12_Servo_X_Ext_End)
                    return eSequenceResult.SUCCESS;
                break;
            case eStep.STEP_20_PUSHER_RETRACT:
            case eStep.Step_21_Servo_X_Ret_Start:
            case eStep.Step_22_Servo_X_Ret_End:
                if (_currentStep == (int)eStep.Step_12_Servo_X_Ext_End)
                    return eSequenceResult.SUCCESS;
                break;
            case eStep.END:
                break;


        }
        return eSequenceResult.BUSY;
    }

    private void InitStep()
    {
        #region AUTO_RUN
        actionUnitStep.Add((int)eStep.START, new UnitStep
        {
            NextStep = () =>
            {
                return (int)eStep.STEP_1_CHECK_READY;
            }
        });
        actionUnitStep.Add((int)eStep.STEP_1_CHECK_READY, new UnitStep
        {
            NextStep = () =>
            {
                return (int)eStep.Step_2_Check_Servo_X_Ready;
            }
        });
        actionUnitStep.Add((int)eStep.Step_2_Check_Servo_X_Ready, new UnitStep
        {
            ExecuteAction = () =>
            {
                return _pusherBase.MoveToReady();
            },
            IsStateOK = () =>
            {
                return _pusherBase.IsPusherReady();
            },
            NextStep = () =>
            {
                return (int)eStep.STEP_5_DIVERGENCE;
            }
        });
        actionUnitStep.Add((int)eStep.STEP_5_DIVERGENCE, new UnitStep
        {
             NextStep = () =>
             {
                 _pusherActionReady?.Invoke(true);
                 return (int)eStep.STEP_10_PUSHER_EXTEND;

             }
        });
        actionUnitStep.Add((int)eStep.STEP_10_PUSHER_EXTEND, new UnitStep
        {
            CheckCondition = () =>
            {
                if (_pusherBase.IsPushReserved())
                {
                    return true;
                }
                return false;
            },
            NextStep = () =>
            {
                return (int)eStep.Step_11_Servo_X_Ext_Start;
            }
        });
        actionUnitStep.Add((int)eStep.Step_11_Servo_X_Ext_Start, new UnitStep
        {
            ExecuteAction = () =>
            {
                _pusherActionReady?.Invoke(false);
                return _pusherBase.MoveToExtend();
            },
            IsStateOK = () =>
            {
                return _pusherBase.IsPusherExtend();
            },
            ActionTimeout = () =>
            {
                SetAlarm(_config.ServorX_AlarmStartCode + (int)eMotorAlarmType.TIMEOUT);
            },
            NextStep = () =>
            {
                return (int)eStep.Step_12_Servo_X_Ext_End;
            }
        });
        actionUnitStep.Add((int)eStep.Step_12_Servo_X_Ext_End, new UnitStep
        {
            NextStep = () =>
            {
                return (int)eStep.STEP_20_PUSHER_RETRACT;
            }
        });
        actionUnitStep.Add((int)eStep.STEP_20_PUSHER_RETRACT, new UnitStep
        {
            NextStep = () =>
            {
                return (int)eStep.Step_21_Servo_X_Ret_Start;
            }
        });
        actionUnitStep.Add((int)eStep.Step_21_Servo_X_Ret_Start, new UnitStep
        {
            ExecuteAction = () =>
            {
                return _pusherBase.MoveToRetract();
            },
            IsStateOK = () =>
            {
                return _pusherBase.IsPusherRetract();
            },
            ActionTimeout = () =>
            {
                SetAlarm(_config.ServorX_AlarmStartCode + (int)eMotorAlarmType.TIMEOUT);
            },
            NextStep = () =>
            {
                return (int)eStep.Step_22_Servo_X_Ret_End;
            }
        });
        actionUnitStep.Add((int)eStep.Step_22_Servo_X_Ret_End, new UnitStep
        {
            ExecuteAction = () =>
            {
                _pusherActionConmplete?.Invoke(true);
                return true;
            },
            NextStep = () =>
            {
                if (_pusherBase.IsJobComplete())
                {

                    return (int)eStep.END;
                }
                else
                {
                    return (int)eStep.STEP_5_DIVERGENCE;
                }

            }
        });
        actionUnitStep.Add((int)eStep.END, new UnitStep()
        {
            NextStep = () =>
            {
                _logger.LogDebug(string.Format("Seq {0} Completed", LogHead));
                return (int)eStep.START;
            }
        });
        #endregion

        #region INIT_RUN
        actionUnitStep.Add((int)eStepInit.START, new UnitStep
        {
            NextStep = () =>
            {
                return (int)eStepInit.STEP_10_SET_INITIALIZE;
            }
        });
        actionUnitStep.Add((int)eStepInit.STEP_10_SET_INITIALIZE, new UnitStep
        {
            NextStep = () =>
            {
                return (int)eStepInit.Step_11_Servo_X_Home;
            }
        });
        actionUnitStep.Add((int)eStepInit.Step_11_Servo_X_Home, new UnitStep
        {
            ExecuteAction = () =>
            {
                return _pusherBase.MoveToHome();
            },
            IsStateOK = () =>
            {
                return _pusherBase.IsPusherHomed();
            },
            ActionTimeout = () =>
            {
                SetAlarm(_config.ServorX_AlarmStartCode + (int)eMotorAlarmType.HOME_START_FAIL);
            },
            NextStep = () =>
            {
                return (int)eStepInit.Step_12_Servo_X_Ready;
            }
        });
        actionUnitStep.Add((int)eStepInit.Step_12_Servo_X_Ready, new UnitStep
        {
            ExecuteAction = () =>
            {
                return _pusherBase.MoveToReady();
            },
            IsStateOK = () =>
            {
                return _pusherBase.IsPusherReady();
            },
            ActionTimeout = () =>
            {
                SetAlarm(_config.ServorX_AlarmStartCode + (int)eMotorAlarmType.TIMEOUT);
            },
            NextStep = () =>
            {
                return (int)eStepInit.END;
            }
        });
        #endregion
    }
    private enum eStep
    {
        IDLE = -1,

        START = 0,

        STEP_1_CHECK_READY = 1,
            Step_2_Check_Servo_X_Ready,

        STEP_5_DIVERGENCE = 5,

        STEP_10_PUSHER_EXTEND = 10,
            Step_11_Servo_X_Ext_Start,
            Step_12_Servo_X_Ext_End,


        STEP_20_PUSHER_RETRACT = 20,
            Step_21_Servo_X_Ret_Start,
            Step_22_Servo_X_Ret_End,

        END
    }

    private enum eStepInit
    {
        IDLE,

        START = 100,

        STEP_10_SET_INITIALIZE = 110,
        Step_11_Servo_X_Home,
        Step_12_Servo_X_Ready,

        END = 200,
    }
}