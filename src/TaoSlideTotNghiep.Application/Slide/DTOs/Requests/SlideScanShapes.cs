using TaoSlideTotNghiep.Application.Base.DTOs;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Requests;

public record SlideScanShapes(string FilePath)
    : Request(SlideRequestType.ScanShapes),
        IFilePathBased;