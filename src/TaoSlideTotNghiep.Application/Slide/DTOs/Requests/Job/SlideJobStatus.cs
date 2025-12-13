using TaoSlideTotNghiep.Application.Base.DTOs;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Requests.Job;

public record SlideJobStatus(string JobId)
    : Request(SlideRequestType.JobStatus),
        IJobBased;