using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Global;

/// <summary>
///     Response containing all group summaries.
/// </summary>
public sealed record SlideGlobalGetGroupsSuccess(IReadOnlyList<GroupSummary> Groups)
    : Response("getallgroups");