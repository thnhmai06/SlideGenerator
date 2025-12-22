using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Domain.Job.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Job;

/// <summary>
///     Response for sheet job status query.
/// </summary>
public sealed record SlideJobStatusSuccess(
    string JobId,
    string SheetName,
    SheetJobStatus Status,
    int CurrentRow,
    int TotalRows,
    float Progress,
    string OutputPath,
    string? ErrorMessage,
    int ErrorCount = 0,
    string? HangfireJobId = null)
    : Response("jobstatus");