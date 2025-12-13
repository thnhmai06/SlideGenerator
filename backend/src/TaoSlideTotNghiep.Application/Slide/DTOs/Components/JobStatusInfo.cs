using TaoSlideTotNghiep.Domain.Sheet.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Components;

public record JobStatusInfo(
    string JobId,
    string SheetName,
    SheetJobStatus Status,
    int CurrentRow,
    int TotalRows,
    float Progress,
    string? ErrorMessage = null);