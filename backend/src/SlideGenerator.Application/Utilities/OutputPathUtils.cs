namespace SlideGenerator.Application.Utilities;

/// <summary>
///     Provides helpers for normalizing output paths for slide generation.
/// </summary>
public static class OutputPathUtils
{
    /// <summary>
    ///     Normalizes output path to a directory (accepts .pptx file path or folder path).
    /// </summary>
    public static string NormalizeOutputFolderPath(string outputPath)
    {
        var fullPath = Path.GetFullPath(outputPath);
        if (Path.HasExtension(fullPath) &&
            string.Equals(Path.GetExtension(fullPath), ".pptx", StringComparison.OrdinalIgnoreCase))
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
                return directory;
        }

        return fullPath;
    }
}