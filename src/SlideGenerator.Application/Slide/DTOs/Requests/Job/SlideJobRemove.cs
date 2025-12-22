namespace SlideGenerator.Application.Slide.DTOs.Requests.Job;

/// <summary>
///     Request to remove a completed sheet job.
/// </summary>
public sealed record SlideJobRemove(string JobId);