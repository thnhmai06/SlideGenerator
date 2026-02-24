namespace SlideGenerator.Generating.Models;

/// <summary>
///     Represents a generation request payload consumed by jobs endpoint.
/// </summary>
/// <param name="Sheet">Spreadsheet source information.</param>
/// <param name="TemplateMap">Sheet-to-template-slide mapping.</param>
/// <param name="TextConfigs">Text replacement bindings.</param>
/// <param name="ImageConfigs">Image replacement bindings.</param>
/// <param name="SaveFolder">Save folder for generated presentations.</param>
public sealed record GenerateSlidesRequest(
    SheetConfig Sheet,
    IReadOnlyDictionary<string, TemplateSlide> TemplateMap,
    IReadOnlyList<TextConfig> TextConfigs,
    IReadOnlyList<ImageConfig> ImageConfigs,
    string SaveFolder);