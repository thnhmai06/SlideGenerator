using TaoSlideTotNghiep.Application.Base.DTOs;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Requests.Job;

public record GenerateSlideJobControlRequest(string JobId, ControlAction Action)
    : Request(SlideRequestType.JobControl),
        IJobBased;