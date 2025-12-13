using TaoSlideTotNghiep.Application.Slide.DTOs.Components;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;
using TaoSlideTotNghiep.Domain.Sheet.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Notifications;

/// <summary>
/// Notification sent when group status changes.
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