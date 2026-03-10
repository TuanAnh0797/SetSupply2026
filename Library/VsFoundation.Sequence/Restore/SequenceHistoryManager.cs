namespace VsFoundation.Sequence.Restore;

public class SequenceHistoryManager
{
    private Stack<SequenceRestore> _history = new();

    public void Save(SequenceRestore snapshot)
    {
        _history.Push(snapshot);
    }

    public SequenceRestore? Undo()
    {
        if (_history.Count == 0)
            return null;

        var snapshot = _history.Pop();
        return snapshot;
    }

    public void Clear() => _history.Clear();
}
