using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Group;

/// <summary>
///     Response for group control actions.
/// </summary>
public sealed record SlideGroupControlSuccess(string GroupId, ControlAction Action)
    : Response("groupcontrol");