using TaoSlideTotNghiep.Application.Base.DTOs;
using TaoSlideTotNghiep.Application.Slide.DTOs.Components;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Responses.Successes;

public record SlideScanShapesSuccess(string FilePath, ShapeDto[]? Shapes = null)
    : Success(SlideRequestType.ScanShapes),
        IFilePathBased;