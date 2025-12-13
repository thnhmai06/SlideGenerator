using SlideGenerator.Application.Base.DTOs;
using SlideGenerator.Application.Slide.DTOs.Components;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes;

public record SlideScanShapesSuccess(string FilePath, ShapeDto[]? Shapes = null)
    : Success(SlideRequestType.ScanShapes),
        IFilePathBased;