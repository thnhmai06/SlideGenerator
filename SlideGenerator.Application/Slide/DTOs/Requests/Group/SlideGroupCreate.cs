using SlideGenerator.Application.Base.DTOs;
using SlideGenerator.Application.Slide.DTOs.Enums;
using SlideGenerator.Domain.Slide.Components;

namespace SlideGenerator.Application.Slide.DTOs.Requests.Group;

public record GenerateSlideGroupCreate(
    string TemplatePresentationPath,
    string SpreadsheetPath,
    TextConfig[] TextConfigs,
    ImageConfig[] ImageConfigs,
    string FilePath,
    string[]? SheetNames) : Request(SlideRequestType.GroupCreate),
    IFilePathBased;