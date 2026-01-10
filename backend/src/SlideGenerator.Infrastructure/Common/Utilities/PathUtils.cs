using System.Collections.Immutable;

namespace SlideGenerator.Infrastructure.Common.Utilities;

/// <summary>
///     Provides utility methods for working with file system paths and file names.
/// </summary>
internal static class PathUtils
{
    private static IImmutableSet<char> InvalidPathChars { get; } =
        ImmutableHashSet.Create(Path.GetInvalidPathChars());

    /// <summary>
    ///     Removes invalid path characters from the specified file name and returns a sanitized version suitable for use as
    ///     a file name.
    /// </summary>
    /// <remarks>
    ///     This method removes any characters from the input that are considered invalid for file paths,
    ///     as defined by the application's configuration. The returned file name is trimmed of leading and trailing
    ///     whitespace.
    /// </remarks>
    /// <param name="fileName">The file name to sanitize. Cannot be null.</param>
    /// <param name="replacement">The character to replace invalid path characters with. Defaults to underscore ('_').</param>
    /// <returns>
    ///     A sanitized file name with all invalid path characters removed. Returns "unnamed" if the resulting file name is
    ///     empty or consists only of whitespace.
    /// </returns>
    public static string SanitizeFileName(string fileName, char replacement = '_')
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "unnamed";

        var buffer = new char[fileName.Length];
        var length = 0;

        foreach (var c in fileName)
            buffer[length++] = InvalidPathChars.Contains(c)
                ? replacement
                : c;

        var result = new string(buffer, 0, length).Trim();
        return result.Length == 0 ? "unnamed" : result;
    }
}