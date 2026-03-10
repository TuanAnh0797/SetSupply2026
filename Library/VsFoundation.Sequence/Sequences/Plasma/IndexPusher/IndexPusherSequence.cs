using VsFoundation.Sequence.Bases;
using VsFoundation.Sequence.Restore;
using VsFoundation.Base.Logger.Interfaces;
using VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Configs;
using VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Controller.Interfaces;
using VsFoundation.Base.Constants.Sequence;

namespace VsFoundation.Sequence.Sequences.Plasma.IndexPusher;

public class IndexPusherSequence : BaseSequence
{
    public override int ModuleId { get; set; } = (int)eSequence.INDEX_PUSHER;
    public override string ModuleName => "LOADING INDEX PUSHER";
    public override string LogHead => "LOAD_INDEX_PUSHER";
    public override bool CanInitialize => true;

    private readonly IndexPusherConfig _config;
    private readonly ILoggingService _logger;
    private readonly IPusherConditions _pusherConditions;
    private readonly IPusherBase _pusherBase;

    public IndexPusherSequence(IndexPusherConfig config, ILoggingService logger, IPusherBase pusherBase, IPusherConditions pusherConditions )
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pusherBase = pusherBase ?? throw new ArgumentNullException(nameof(pusherBase));
        _pusherConditions = pusherConditions ?? throw new ArgumentNullException(nameof(pusherConditions));
    }

    protected override bool Restore(SequenceRestore state)
    {
        return true;
    }

    protected override void Savestore(SequenceRestore state)
    {

    }

    public override void Stop()
    {
        base.Stop();
    }

    public override void ClearAlarm()
    {
        base.ClearAlarm();
    }

    public override void FirstHomeCmd()
    {
        base.FirstHomeCmd();
    }

    public override void AlwaysRun(eSeqState state)
    {
        base.AlwaysRun(state);
    }

    public override bool Initialize()
    {
        if (IsInitialized) return true;

        return IsInitialized;
    }

    protected override void SetAlarm(int nErrorCode)
    {
        _logger.LogError(string.Format("{0}: Error No.{1} Step {2}", LogHead, nErrorCode, _currentStep));
        base.SetAlarm(nErrorCode);
    }

    public override string GetStepName(int step) => ((eStep)step).ToString();

    public override eSequenceResult RunSequence()
    {
        throw new NotImplementedException();
    }

    private enum eStep
    {
        IDLE = -1,

        START = 0,

        END
    }

    private enum eStepInit
    {
        START = 9000,

        END
    }
}
