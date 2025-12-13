using TaoSlideTotNghiep.Application.Base.DTOs;
using TaoSlideTotNghiep.Application.Slide.DTOs.Enums;
using TaoSlideTotNghiep.Domain.Slide.Components;

namespace TaoSlideTotNghiep.Application.Slide.DTOs.Requests.Group;

public record GenerateSlideGroupCreate(
    string TemplatePresentationPath,
    string SpreadsheetPath,
    TextConfig[] TextConfigs,
    ImageConfig[] ImageConfigs,
    string FilePath,
    string[]? SheetNames) : Request(SlideRequestType.GroupCreate),
    IFilePathBased;