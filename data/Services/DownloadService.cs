using TaoSlideTotNghiep.Models;

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
        Logger.LogInformation("Creating download task: {Url} -> {SavePath}", url, savePath);
        return new ImageDownloadTask(url, savePath);
    }

    public async Task StartDownloadAsync(ImageDownloadTask task, HttpClient? httpClient = null)
    {
        Logger.LogInformation("Starting download: {Url}", task.Url);
        await task.StartAsync(httpClient);
    }
}
