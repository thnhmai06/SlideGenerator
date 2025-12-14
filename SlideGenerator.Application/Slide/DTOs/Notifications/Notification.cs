using SlideGenerator.Application.Base.DTOs.Enums;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Notifications;

/// <summary>
///     Base notification for Slide related events.
/// </summary>
public abstract record Notification(SlideRequestType Type)
    : Base.DTOs.Notifications.Notification(RequestType.Slide);