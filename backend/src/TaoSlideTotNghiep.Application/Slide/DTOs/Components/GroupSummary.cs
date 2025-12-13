using TaoSlideTotNghiep.Domain.Sheet.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Components;

public record GroupSummary(
    string GroupId,
    string WorkbookPath,
    GroupStatus Status,
    float Progress,
    int TotalJobs,
    int CompletedJobs);