namespace SlideGenerator.Application.Services.Generating.Rules;

public static class NamingRules
{
    public const string DefaultWorkbookName = "unnamed_workbook";
    public const string DefaultWorksheetName = "unnamed_worksheet";

    /// <summary>
    ///     Normalizes a file name by replacing invalid characters with underscores.
    /// </summary>
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