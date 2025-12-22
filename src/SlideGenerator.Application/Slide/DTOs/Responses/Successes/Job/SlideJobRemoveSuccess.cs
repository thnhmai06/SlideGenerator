using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Job;

/// <summary>
///     Response for removing a completed sheet job.
/// </summary>
public sealed record SlideJobRemoveSuccess(string JobId, bool Removed)
    : Response("jobremove");