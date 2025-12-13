using TaoSlideTotNghiep.Application.Slide.DTOs.Components;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;
using TaoSlideTotNghiep.Domain.Sheet.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Successes.Group;

public record SlideGroupStatusSuccess(
    string GroupId,
    GroupStatus Status,
    float Progress,
    Dictionary<string, JobStatusInfo> Jobs)
    : Success(SlideRequestType.GroupStatus);