using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Domain.Job.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Group;

/// <summary>
///     Response for group status query.
/// </summary>
public sealed record SlideGroupStatusSuccess(
    string GroupId,
    GroupStatus Status,
    float Progress,
    IReadOnlyDictionary<string, JobStatusInfo> Jobs,
    int ErrorCount = 0)
    : Response("groupstatus");