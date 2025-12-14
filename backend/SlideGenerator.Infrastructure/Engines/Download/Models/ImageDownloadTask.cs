using Downloader;
using SlideGenerator.Application.Configs.Models;

namespace SlideGenerator.Infrastructure.Engines.Download.Models;

/// <summary>
///     Represents an Image download task.
/// </summary>
public class ImageDownloadTask(string url, string savePath) : DownloadTask(url, savePath)
{
    protected override Exception? ValidateDownload(DownloadStartedEventArgs e)
    {
        var extension = Path.GetExtension(e.FileName).TrimStart('.');
        if (string.IsNullOrEmpty(extension) || !Config.ImageExtensions.Contains(extension))
            return new ArgumentException($"Image extension '{extension}' is not supported.", nameof(extension));
        return null;
    }
}