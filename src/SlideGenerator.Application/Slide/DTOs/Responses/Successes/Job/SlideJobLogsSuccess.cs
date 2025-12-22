using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Job;

/// <summary>
///     Log entry payload for job logs.
/// </summary>
public sealed record JobLogEntryDto(
    string Level,
    string Message,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object?>? Data = null);

/// <summary>
///     Response for job log retrieval.
/// </summary>
public sealed record SlideJobLogsSuccess(
    string JobId,
    IReadOnlyList<JobLogEntryDto> Logs)
    : Response("joblogs");