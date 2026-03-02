using SlideGenerator.Services.Generating.Models.Configs;
using SlideGenerator.Services.Generating.Models.Info;

namespace SlideGenerator.Services.Generating.Models;

/// <summary>
///     Represents a generation request payload consumed by endpoint.
/// </summary>
/// <param name="Book">Book source information.</param>
/// <param name="SheetToSlideMap">Sheet-to-template-slide mapping.</param>
/// <param name="TextConfigs">Text replacement bindings.</param>
/// <param name="ImageConfigs">Image replacement bindings.</param>
/// <param name="SaveFolder">Save folder for generated presentations.</param>
/// <remarks>
///     Reviewed by @thnhmai06 at 01/03/2026 00:58:42 GMT+7
/// </remarks>
public sealed record SlidesGenerateRequest(
    BookInfo Book,
    IReadOnlyDictionary<string, SlideInfo> SheetToSlideMap, // sheet name -> slide
    IReadOnlyList<TextConfig> TextConfigs,
    IReadOnlyList<ImageConfig> ImageConfigs,
    string SaveFolder);