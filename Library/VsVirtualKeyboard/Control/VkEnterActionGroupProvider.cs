using System.Windows;
using System.Windows.Controls;
using VsVirtualKeyboard.Provider;

namespace VsVirtualKeyboard.Control;

public class VkEnterActionGroupProvider : ContentControl
{
    public string GroupId
    {
        get => (string)GetValue(GroupIdProperty);
        set => SetValue(GroupIdProperty, value);
    }

    public static readonly DependencyProperty GroupIdProperty =
        DependencyProperty.Register(nameof(GroupId), typeof(string), typeof(VkEnterActionGroupProvider), new PropertyMetadata(null));

    public IEnterActionProvider? Commit
    {
        get => (IEnterActionProvider?)GetValue(CommitProperty);
        set => SetValue(CommitProperty, value);
    }

    public static readonly DependencyProperty CommitProperty = DependencyProperty.Register(nameof(Commit), typeof(IEnterActionProvider), typeof(VkEnterActionGroupProvider), new PropertyMetadata(null));
}
