namespace SlideGenerator.Application.Tasks;

/// <summary>
///     Utility functions for workflow tasks.
/// </summary>
public static class Utilities
{
    /// <summary>
    ///     Normalizes a file name by replacing invalid characters with underscores.
    /// </summary>
    /// <param name="value">The file name to normalize.</param>
    /// <returns>Normalized file name safe for file system. Returns empty string if input is empty/null.</returns>
    public static string NormalizeFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim();
        normalized = Path.GetInvalidFileNameChars()
            .Aggregate(normalized, (current, invalid) => current.Replace(invalid, '_'));

        return string.IsNullOrWhiteSpace(normalized) ? string.Empty : normalized;
    }
}