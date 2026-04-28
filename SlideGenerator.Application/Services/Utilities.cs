namespace SlideGenerator.Application.Services;

public static class Utilities
{
    /// <summary>
    ///     Normalizes a raw string value into a valid URI.
    /// </summary>
    /// <param name="value">The raw string value (e.g., from a spreadsheet cell).</param>
    /// <returns>A normalized <see cref="Uri" />, or <see langword="null" /> if the value is invalid or not a link.</returns>
    /// <remarks>
    ///     If the value does not contain a scheme (e.g., "example.com/image.png"), "https://" is automatically prepended.
    /// </remarks>
    public static Uri? NormalizeUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        // If it doesn't look like a URL with a scheme, prepend https://
        if (!trimmed.Contains("://")) trimmed = "https://" + trimmed;
        // maybe we can ignore url like "https://link" here, but technically, it is possible (internet does not have that)

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ? uri : null;
    }

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