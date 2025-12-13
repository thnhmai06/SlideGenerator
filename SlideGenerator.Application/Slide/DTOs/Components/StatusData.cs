using SlideGenerator.Domain.Sheet.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Components;

/// <summary>
/// Shared data for job status information.
/// Used by both JobStatusNotification and SlideJobStatusSuccess.
/// </summary>
public record JobStatusData(string JobId, SheetJobStatus Status, string? Message = null);

/// <summary>
/// Shared data for group status information.
/// Used by both GroupStatusNotification and SlideGroupStatusSuccess.
/// </summary>
public record GroupStatusData(string GroupId, GroupStatus Status, string? Message = null);