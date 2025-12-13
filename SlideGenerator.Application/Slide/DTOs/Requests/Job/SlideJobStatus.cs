using SlideGenerator.Application.Base.DTOs;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Requests.Job;

public record SlideJobStatus(string JobId)
    : Request(SlideRequestType.JobStatus),
        IJobBased;