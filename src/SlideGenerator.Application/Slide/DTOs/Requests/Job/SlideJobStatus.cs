namespace SlideGenerator.Application.Slide.DTOs.Requests.Job;

/// <summary>
///     Request to query a sheet job status.
/// </summary>
public sealed record SlideJobStatus(string JobId);