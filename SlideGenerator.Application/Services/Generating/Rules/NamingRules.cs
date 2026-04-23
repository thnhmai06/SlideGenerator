namespace SlideGenerator.Application.Services.Generating.Rules;

/// <summary>
///     Provides naming conventions and normalization logic for files and directories.
/// </summary>
public static class NamingRules
{
    /// <summary>The default name used when a workbook name is missing.</summary>
    public const string DefaultWorkbookName = "unnamed_workbook";

    /// <summary>The default name used when a worksheet name is missing.</summary>
    public const string DefaultWorksheetName = "unnamed_worksheet";

    /// <summary>
    ///     Normalizes a file name by replacing invalid characters with underscores.
    /// </summary>
    /// <param name="value">The original string to normalize.</param>
    /// <param name="defaultValue">The default value to return if the input is null, empty, or whitespace.</param>
    /// <returns>A safe string suitable for use as a file or directory name.</returns>
    public static string NormalizeFileName(string? value, string? defaultValue = null)
    {
        defaultValue ??= string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        var normalized = value.Trim();
        normalized = Path.GetInvalidFileNameChars()
            .Aggregate(normalized, (current, invalid) => current.Replace(invalid, '_'));

        return string.IsNullOrWhiteSpace(normalized) ? defaultValue : normalized;
    }
}