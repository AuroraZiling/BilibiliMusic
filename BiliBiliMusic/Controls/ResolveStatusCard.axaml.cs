using Avalonia;
using Avalonia.Controls.Primitives;
using BiliBiliMusic.Core.Models;

namespace BiliBiliMusic.Controls;

public class ResolveStatusCard : TemplatedControl
{
    public static readonly StyledProperty<ResolveStatus> StatusProperty =
        AvaloniaProperty.Register<ResolveStatusCard, ResolveStatus>(nameof(Status), defaultValue: 0);

    public ResolveStatus Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }
    
    public static readonly StyledProperty<string> StatusDescriptionProperty =
        AvaloniaProperty.Register<ResolveStatusCard, string>(nameof(StatusDescription), defaultValue: "");

    public string StatusDescription
    {
        get => GetValue(StatusDescriptionProperty);
        set => SetValue(StatusDescriptionProperty, value);
    }
    
    public static readonly StyledProperty<string> VideoTitleProperty =
        AvaloniaProperty.Register<ResolveStatusCard, string>(nameof(VideoTitle), defaultValue: "");

    public string VideoTitle
    {
        get => GetValue(VideoTitleProperty);
        set => SetValue(VideoTitleProperty, value);
    }
}