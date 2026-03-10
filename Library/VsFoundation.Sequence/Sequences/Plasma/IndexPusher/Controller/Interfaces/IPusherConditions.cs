using VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Constants;

namespace VsFoundation.Sequence.Sequences.Plasma.IndexPusher.Controller.Interfaces;

public interface IPusherConditions
{
    ConditionResult IsReadyForLoad(int lane);
    ConditionResult IsReadyForUnload(int lane);
    ConditionResult CanExecute(PusherJobType jobType, int lane);
    IReadOnlyList<PusherDemand> GetDemands();
}
