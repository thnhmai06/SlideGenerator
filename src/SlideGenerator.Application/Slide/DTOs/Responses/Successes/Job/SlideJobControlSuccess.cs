using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Job;

/// <summary>
///     Response for sheet job control actions.
/// </summary>
public sealed record SlideJobControlSuccess(string JobId, ControlAction Action)
    : Response("jobcontrol");