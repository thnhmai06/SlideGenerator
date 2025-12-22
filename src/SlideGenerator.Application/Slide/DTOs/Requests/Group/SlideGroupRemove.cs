namespace SlideGenerator.Application.Slide.DTOs.Requests.Group;

/// <summary>
///     Request to remove a completed group job.
/// </summary>
public sealed record SlideGroupRemove(string GroupId);