using SlideGenerator.Application.Download.Rules;

namespace SlideGenerator.Application.Services.Generating.Rules;

/// <summary>
///     Defines constant rules for image folder paths used during generation.
/// </summary>
public static class ImagePathRules
{
    /// <summary>The folder name for downloaded images.</summary>
    public const string DownloadedFolder = "Downloaded";

    /// <summary>The folder name for edited images.</summary>
    public const string EditedFolder = "Edited";

    /// <summary>
    ///     Scans the directory of <paramref name="basePath" /> for the first file whose base name
    ///     matches and whose extension is not a temporary download marker.
    ///     Returns <see langword="null" /> if the directory does not exist or no match is found.
    /// </summary>
    public static string? ScanDownloadedFile(string basePath)
    {
        var directory = Path.GetDirectoryName(basePath);
        var fileName = Path.GetFileName(basePath);

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return null;

        return Directory
            .GetFiles(directory, $"{fileName}.*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => !string.Equals(
                Path.GetExtension(f), FileExtensionRules.TempDownload, StringComparison.OrdinalIgnoreCase));
    }
}