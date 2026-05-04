using SlideGenerator.Services.Generating.Models;
using SlideGenerator.Services.Generating.Models.Identifiers;

namespace SlideGenerator.Services.Generating.Workflows.Models;

/// <summary>
/// Represents a comprehensive task for downloading and editing a single image.
/// </summary>
public sealed class ImageTask(
    SheetIdentifier sheet,
    int rowIndex,
    string columnName,
    string shapeName,
    Uri? sourceUri,
    string downloadPath,
    string editPath,
    double width,
    double height,
    EditOptions editOptions,
    string? fallbackImagePath = null)
{
    public SheetIdentifier Sheet { get; } = sheet;
    public int RowIndex { get; } = rowIndex;
    public string ColumnName { get; } = columnName;
    public string ShapeName { get; } = shapeName;
    public Uri? SourceUri { get; } = sourceUri;
    public string DownloadPath { get; } = downloadPath;
    public string EditPath { get; } = editPath;
    public double Width { get; } = width;
    public double Height { get; } = height;
    public EditOptions EditOptions { get; } = editOptions;
    public string? FallbackImagePath { get; } = fallbackImagePath;
}
