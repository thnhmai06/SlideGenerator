using SlideGenerator.Application.Base.DTOs;
using SlideGenerator.Application.Slide.DTOs.Enums;
using SlideGenerator.Domain.Sheet.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Job;

public record SlideJobStatusSuccess(
    string JobId,
    string SheetName,
    SheetJobStatus Status,
    int CurrentRow,
    int TotalRows,
    float Progress,
    string? ErrorMessage = null)
    : Success(SlideRequestType.JobStatus),
        IJobBased;