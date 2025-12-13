namespace TaoSlideTotNghiep.Application.Slide.DTOs.Components;

/// <summary>
/// Shared data for job progress information.
/// Used by both JobProgressNotification and can be embedded in Success responses.
/// </summary>
public record JobProgressData(string JobId, int CurrentRow, int TotalRows, float Progress);

/// <summary>
/// Shared data for group progress information.
/// </summary>
public record GroupProgressData(string GroupId, float Progress);