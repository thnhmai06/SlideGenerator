using SlideGenerator.Application.Slide.DTOs.Components;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Global;

public record SlideGlobalGetGroupsSuccess(List<GroupSummary> Groups)
    : Success(SlideRequestType.GetAllGroups);