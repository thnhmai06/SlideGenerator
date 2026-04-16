using Elsa.Expressions.Models;

namespace SlideGenerator.Application.Workflows;

/// <summary>
///     Utility functions for workflow tasks.
/// </summary>
public static class Utilities
{
    /// <summary>
    ///     Normalizes a file name by replacing invalid characters with underscores.
    /// </summary>
    /// <param name="value">The file name to normalize.</param>
    /// <param name="defaultValue">The default value to return if the input is null or whitespace. Defaults to empty string.</param>
    /// <returns>Normalized file name safe for file system. Returns default string if input is empty/null.</returns>
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

    public static MemoryBlockReference GetRef(string name)
    {
        return new MemoryBlockReference(name);
    }
}