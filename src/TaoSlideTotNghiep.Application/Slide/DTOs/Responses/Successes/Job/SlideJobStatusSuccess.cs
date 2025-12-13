using TaoSlideTotNghiep.Application.Base.DTOs;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;
using TaoSlideTotNghiep.Domain.Sheet.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Successes.Job;

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