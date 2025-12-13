using SlideGenerator.Application.Base.DTOs;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Requests.Job;

public record GenerateSlideJobControlRequest(string JobId, ControlAction Action)
    : Request(SlideRequestType.JobControl),
        IJobBased;