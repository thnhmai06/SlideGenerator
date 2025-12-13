using SlideGenerator.Application.Slide.DTOs.Components;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Notifications;

/// <summary>
/// Notification sent when group progress is updated.
/// </summary>
public record GroupProgressNotification(GroupProgressData Data)
    : Notification(SlideRequestType.GroupStatus)
{
    // Convenience constructor
    public GroupProgressNotification(string groupId, float progress)
        : this(new GroupProgressData(groupId, progress))
    {
    }

    // Expose for backward compatibility
    public string GroupId => Data.GroupId;
    public float Progress => Data.Progress;
}