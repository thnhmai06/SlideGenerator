using TaoSlideTotNghiep.Application.Base.DTOs;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Successes.Job;

public record SlideJobControlSuccess(string JobId, ControlAction Action)
    : Success(SlideRequestType.JobControl),
        IJobBased;