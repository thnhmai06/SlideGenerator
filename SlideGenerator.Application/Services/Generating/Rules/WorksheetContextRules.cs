namespace SlideGenerator.Application.Services.Generating.Rules;

/// <summary>
///     Defines canonical names for the workflow variables used within a worksheet-generation run.
///     Pass these names when creating <c>Variable&lt;T&gt;</c> instances so they appear in logs
///     and diagnostics under a consistent, human-readable label.
/// </summary>
public static class WorksheetContextRules
{
    /// <summary>Key for the context variable.</summary>
    public const string Context = "Ctx";

    /// <summary>Key for the worksheet identifier.</summary>
    public const string Worksheet = "Worksheet";

    /// <summary>Key for the current row index.</summary>
    public const string Row = "Row";

    /// <summary>Key for the total row count.</summary>
    public const string RowCount = "RowCount";

    /// <summary>Key for the current download item.</summary>
    public const string DownloadItem = "DownloadItem";

    /// <summary>Key for the current edit item.</summary>
    public const string EditItem = "EditItem";

    /// <summary>Key for the output path.</summary>
    public const string OutputPath = "OutputPath";

    /// <summary>Key for the working template slide identifier.</summary>
    public const string WorkingTemplateSlide = "WorkingTemplateSlide";

    /// <summary>Key for the set of template placeholders.</summary>
    public const string TemplatePlaceholders = "TemplatePlaceholders";

    /// <summary>Key for the set of template image shape IDs.</summary>
    public const string TemplateImageShapeIds = "TemplateImageShapeIds";

    /// <summary>Key for the specialized text instructions.</summary>
    public const string TextInstructions = "TextInstructions";

    /// <summary>Key for the specialized image instructions.</summary>
    public const string ImageInstructions = "ImageInstructions";

    /// <summary>Key for the dictionary of resolved image URLs.</summary>
    public const string ResolvedImageUrls = "ResolvedImageUrls";

    /// <summary>Key for the dictionary of downloaded image paths.</summary>
    public const string DownloadedImagePaths = "DownloadedImagePaths";

    /// <summary>Key for the dictionary of edited image paths.</summary>
    public const string EditedImagePaths = "EditedImagePaths";

    /// <summary>Key for the presentation lease disposable.</summary>
    public const string PresentationLease = "PresentationLease";
}