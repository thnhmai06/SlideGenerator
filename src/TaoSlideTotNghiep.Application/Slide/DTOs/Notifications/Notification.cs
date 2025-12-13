using TaoSlideTotNghiep.Application.Base.DTOs.Enums;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Notifications;

/// <summary>
/// Base notification for Slide related events.
/// </summary>
public abstract record Notification(SlideRequestType Type)
    : Base.DTOs.Notifications.Notification(RequestType.Slide);