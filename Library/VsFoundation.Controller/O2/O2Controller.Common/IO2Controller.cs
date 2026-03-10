namespace VsFoundation.Controller.O2.O2Controller.Common;

public interface IO2Controller
{
    bool IsOpen { get; }
    Task<bool> Open();
    Task Close();
    Task<bool> Init();
    Task<O2Result?> GetOxygen();
}
