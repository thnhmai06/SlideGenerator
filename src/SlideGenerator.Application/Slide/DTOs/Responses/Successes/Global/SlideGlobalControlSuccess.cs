using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Global;

/// <summary>
///     Response for global control actions.
/// </summary>
public sealed record SlideGlobalControlSuccess(ControlAction Action, int AffectedGroups, int AffectedJobs)
    : Response("globalcontrol");