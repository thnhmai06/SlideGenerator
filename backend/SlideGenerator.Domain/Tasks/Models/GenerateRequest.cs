namespace SlideGenerator.Domain.Tasks.Models;

/// <summary>
///     Represents a generation request payload consumed by endpoint.
/// </summary>
/// <param name="WorkbookPath">Workbook file path.</param>
/// <param name="SheetToSlideMap">Sheet-to-template-slide mapping.</param>
/// <param name="TextConfigs">Text replacement bindings.</param>
/// <param name="ImageConfigs">Image replacement bindings.</param>
/// <param name="SaveFolder">Save folder for generated presentations.</param>
/// <remarks>
///     Reviewed by @thnhmai06 at 01/03/2026 00:58:42 GMT+7
/// </remarks>
public sealed record GenerateRequest(
    string WorkbookPath,
    IReadOnlyDictionary<string, SlideInfo> SheetToSlideMap, // sheet name -> slide
    IReadOnlyList<TextConfig> TextConfigs,
    IReadOnlyList<ImageConfig> ImageConfigs,
    string SaveFolder);