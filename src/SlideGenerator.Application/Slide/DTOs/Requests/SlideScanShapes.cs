using SlideGenerator.Application.Base.DTOs;
using SlideGenerator.Application.Slide.DTOs.Enums;

namespace SlideGenerator.Application.Slide.DTOs.Requests;

public record SlideScanShapes(string FilePath)
    : Request(SlideRequestType.ScanShapes),
        IFilePathBased;