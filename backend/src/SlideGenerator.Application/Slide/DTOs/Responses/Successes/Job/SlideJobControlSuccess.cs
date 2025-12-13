using SlideGenerator.Application.Base.DTOs;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Job;

public record SlideJobControlSuccess(string JobId, ControlAction Action)
    : Success(SlideRequestType.JobControl),
        IJobBased;