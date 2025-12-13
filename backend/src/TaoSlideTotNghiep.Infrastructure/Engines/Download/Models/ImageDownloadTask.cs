using Downloader;
using TaoSlideTotNghiep.Application.Configs.Models;
using TaoSlideTotNghiep.Infrastructure.Exceptions.Sheet;

namespace TaoSlideTotNghiep.Infrastructure.Engines.Download.Models;

/// <summary>
/// Represents an Image download task.
/// </summary>
public class ImageDownloadTask(string url, string savePath) : DownloadTask(url, savePath)
{
    protected override Exception? ValidateDownload(DownloadStartedEventArgs e)
    {
        var extension = Path.GetExtension(e.FileName).TrimStart('.');
        if (string.IsNullOrEmpty(extension) || !Config.ImageExtensions.Contains(extension))
            return new FileExtensionNotSupportedException(extension);
        return null;
    }
}