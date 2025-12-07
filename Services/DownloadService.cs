using TaoSlideTotNghiep.Logic;

namespace TaoSlideTotNghiep.Services;

/// <summary>
/// Interface for download service.
/// </summary>
public interface IDownloadService
{
    ImageDownloadTask CreateDownloadTask(string url, string savePath, HttpClient? httpClient = null);
    Task StartDownloadAsync(ImageDownloadTask task, HttpClient? httpClient = null);
}



/// <summary>
/// Download service implementation using Downloader library.
/// </summary>
public class DownloadService(ILogger<DownloadService> logger) : Service(logger), IDownloadService
{
    public ImageDownloadTask CreateDownloadTask(string url, string savePath, HttpClient? httpClient = null)
    {
        logger.LogInformation("Creating download task: {Url} -> {SavePath}", url, savePath);
        return new ImageDownloadTask(url, savePath, httpClient);
    }

    public async Task StartDownloadAsync(ImageDownloadTask task, HttpClient? httpClient = null)
    {
        logger.LogInformation("Starting download: {Url}", task.Url);
        await task.StartAsync(httpClient);
    }
}
