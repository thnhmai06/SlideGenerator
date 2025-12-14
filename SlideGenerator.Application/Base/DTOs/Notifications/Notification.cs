using SlideGenerator.Application.Base.DTOs.Enums;

namespace SlideGenerator.Application.Base.DTOs.Notifications;

/// <summary>
///     Base notification for real-time updates via SignalR.
/// </summary>
/// <param name="RequestType">Type of the request this notification relates to.</param>
public abstract record Notification(RequestType RequestType);