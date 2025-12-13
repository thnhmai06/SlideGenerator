using SlideGenerator.Application.Configs.Models;

namespace SlideGenerator.Infrastructure.Utilities;

public static class PathUtils
{
    public static string SanitizeFileName(string fileName)
    {
        var sanitized = new string(fileName.Where(c => !Config.InvalidPathChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized.Trim();
    }
}