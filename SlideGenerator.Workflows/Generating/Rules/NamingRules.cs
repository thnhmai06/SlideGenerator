using System.Text;

namespace SlideGenerator.Workflows.Generating.Rules;

public static class NamingRules
{
    public static string NormalizeFileName(string? name, string fallback)
    {
        if (string.IsNullOrWhiteSpace(name)) return fallback;

        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    public static string BuildPathSegment(string? displayName, string? hashSource, string fallback)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? fallback : displayName;
        var source = string.IsNullOrWhiteSpace(hashSource) ? fallback : hashSource;
        var clean = NormalizeFileName(name, fallback);
        var hash7 = ComputeBase64Hash(StripSpecialChars(source));
        return $"{clean}_{hash7}";
    }

    public static string BuildPathSegment(string? original, string fallback) => BuildPathSegment(original, original, fallback);

    private static string StripSpecialChars(string value) => new string(value.Where(char.IsLetterOrDigit).ToArray());

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
            .Select(c => Array.IndexOf(invalidChars, c) >= 0 || c is '+' or '/' or '=' ? '-' : c)
            .ToArray());
    }
}