namespace SlideGenerator.Workflows.Generating.Rules;

public static class ImagePathRules
{
    public const string DownloadedFolder = "Downloaded";
    public const string EditedFolder = "Edited";

    public static string? ScanDownloadedFile(string basePath)
    {
        var directory = Path.GetDirectoryName(basePath);
        var fileName = Path.GetFileName(basePath);

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return null;

        return Directory
            .GetFiles(directory, $"{fileName}.*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => !string.Equals(
                Path.GetExtension(f), ".crdownload", StringComparison.OrdinalIgnoreCase));
    }
}