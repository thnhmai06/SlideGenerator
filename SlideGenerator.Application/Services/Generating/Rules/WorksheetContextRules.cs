namespace SlideGenerator.Application.Services.Generating.Rules;

/// <summary>
///     Defines canonical names for the workflow variables used within a worksheet-generation run.
///     Pass these names when creating <c>Variable&lt;T&gt;</c> instances so they appear in logs
///     and diagnostics under a consistent, human-readable label.
/// </summary>
public static class WorksheetContextRules
{
    public const string Context = "Ctx";
    public const string Worksheet = "Worksheet";
    public const string Row = "Row";
    public const string RowCount = "RowCount";
    public const string DownloadItem = "DownloadItem";
    public const string EditItem = "EditItem";

    // New keys for context state
    public const string OutputPath = "OutputPath";
    public const string WorkingTemplateSlide = "WorkingTemplateSlide";
    public const string TemplatePlaceholders = "TemplatePlaceholders";
    public const string TemplateImageShapeIds = "TemplateImageShapeIds";
    public const string TextInstructions = "TextInstructions";
    public const string ImageInstructions = "ImageInstructions";
    public const string ResolvedImageUrls = "ResolvedImageUrls";
    public const string DownloadedImagePaths = "DownloadedImagePaths";
    public const string EditedImagePaths = "EditedImagePaths";
    public const string PresentationLease = "PresentationLease";
}
