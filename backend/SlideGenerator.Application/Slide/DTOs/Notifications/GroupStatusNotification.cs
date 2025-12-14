using SlideGenerator.Application.Slide.DTOs.Components;
using SlideGenerator.Application.Slide.DTOs.Enums;
using SlideGenerator.Domain.Sheet.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Notifications;

/// <summary>
///     Notification sent when group status changes.
/// </summary>
public record GroupStatusNotification(GroupStatusData Data)
    : Notification(SlideRequestType.GroupStatus)
{
    // Convenience constructor
    public GroupStatusNotification(string groupId, GroupStatus status, string? message = null)
        : this(new GroupStatusData(groupId, status, message))
    {
    }

    // Expose for backward compatibility
    public string GroupId => Data.GroupId;
    public GroupStatus Status => Data.Status;
    public string? Message => Data.Message;
}