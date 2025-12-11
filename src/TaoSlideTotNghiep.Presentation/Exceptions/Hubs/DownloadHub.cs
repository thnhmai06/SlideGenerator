namespace TaoSlideTotNghiep.Presentation.Exceptions.Hubs;

/// <summary>
/// Exception thrown when a download task is not found.
/// </summary>
public class DownloadTaskNotFoundException(string filePath)
    : KeyNotFoundException($"Download task for '{filePath}' not found.")
{
    public string FilePath { get; } = filePath;
}