namespace SlideGenerator.Application.Services.Generating.Models;

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

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ? uri : null;
    }
}