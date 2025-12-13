using SlideGenerator.Application.Slide.DTOs.Components;
using SlideGenerator.Application.Slide.DTOs.Enums;
using SlideGenerator.Domain.Sheet.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Group;

public record SlideGroupStatusSuccess(
    string GroupId,
    GroupStatus Status,
    float Progress,
    Dictionary<string, JobStatusInfo> Jobs)
    : Success(SlideRequestType.GroupStatus);