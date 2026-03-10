using System.Windows;
using VsVirtualKeyboard.Model;

namespace VsVirtualKeyboard.Provider;

public interface IEnterActionProvider
{
    public DependencyObject? PlacementTarget { get; set; }
    Task<EnterActionResult> ExecuteAsync();
    object Read();
}
