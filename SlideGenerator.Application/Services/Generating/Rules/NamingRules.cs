using System.Text;

namespace SlideGenerator.Application.Services.Generating.Rules;

/// <summary>
///     Defines naming conventions and defaults for generated files and jobs.
/// </summary>
public static class NamingRules
{
    /// <summary>The default name used for workbooks when one cannot be resolved.</summary>
    public const string DefaultWorkbookName = "Workbook";

    /// <summary>The default name used for worksheets when one cannot be resolved.</summary>
    public const string DefaultWorksheetName = "Worksheet";

    /// <summary>The default name used for columns when one cannot be resolved.</summary>
    public const string DefaultColumnName = "Column";

    /// <summary>
    ///     Normalizes a string to be used as a safe file or folder name.
    /// </summary>
    public static string NormalizeFileName(string? name, string fallback)
    {
        if (string.IsNullOrWhiteSpace(name)) return fallback;

        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    /// <summary>
    ///     Builds a folder-name segment in the form <c>{cleanName}_{hash7}</c> where both the
    ///     display name and the hash source are the same string.
    /// </summary>
    public static string BuildPathSegment(string? original, string fallback)
    {
        return BuildPathSegment(original, original, fallback);
    }

    /// <summary>
    ///     Builds a folder-name segment in the form <c>{cleanDisplayName}_{hash7}</c>, where
    ///     <c>hash7</c> is derived from <paramref name="hashSource" /> (special characters stripped,
    ///     then first 7 characters of the plain Base64 of the UTF-8 bytes).  Use this overload when
    ///     the display name should differ from what is hashed — e.g., showing the workbook file name
    ///     while hashing its full path for uniqueness.
    /// </summary>
    public static string BuildPathSegment(string? displayName, string? hashSource, string fallback)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? fallback : displayName;
        var source = string.IsNullOrWhiteSpace(hashSource) ? fallback : hashSource;
        var clean = NormalizeFileName(name, fallback);
        var hash7 = ComputeBase64Hash(StripSpecialChars(source));
        return $"{clean}_{hash7}";
    }

    /// <summary>Removes every character that is not a letter or digit.</summary>
    private static string StripSpecialChars(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray());
    }

    private static string ComputeBase64Hash(string value, int length = 7)
    {
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        var segment = b64.Length >= length ? b64[..length] : b64;
        return SanitizeBase64Segment(segment);
    }

    private static string SanitizeBase64Segment(string segment)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(segment
            .Select(c => Array.IndexOf(invalidChars, c) >= 0 || c is '+' or '/' or '='
                ? '-'
                : c)
            .ToArray());
    }
}