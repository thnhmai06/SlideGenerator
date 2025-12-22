using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Group;

/// <summary>
///     Response for removing a completed group job.
/// </summary>
public sealed record SlideGroupRemoveSuccess(string GroupId, bool Removed)
    : Response("groupremove");
