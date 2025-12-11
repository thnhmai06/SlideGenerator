using Microsoft.Extensions.Logging;
using TaoSlideTotNghiep.Application.Contracts;
using TaoSlideTotNghiep.Domain.Interfaces;
using TaoSlideTotNghiep.Infrastructure.Engines.Models;
using HttpUtils = TaoSlideTotNghiep.Infrastructure.Utilities.HttpUtils;

namespace TaoSlideTotNghiep.Infrastructure.Services;

/// <summary>
/// Download service implementation using Downloader library.
/// </summary>
public class DownloadService(ILogger<DownloadService> logger) : Service(logger),
    IDownloadService
{
    /// <inheritdoc />
    public async Task<IImageDownloadTask> CreateAndStartDownloadAsync(
        string url,
        string savePath,
        HttpClient httpClient,
        Action<IImageDownloadTask, double> onProgress,
        Action<IImageDownloadTask> onCompleted,
        Action<IImageDownloadTask, Exception> onError)
    {
        Logger.LogInformation("Creating download task: {Url} -> {SavePath}", url, savePath);

        // Correct the URL if needed (Google Drive, OneDrive, etc.)
        var correctedUrl = await HttpUtils.CorrectImageUrlAsync(url, httpClient);

        var task = new ImageDownloadTask(correctedUrl, savePath);

        // Subscribe to progress updates
        task.DownloadProgressChanged += (_, e) => { onProgress(task, e.ProgressPercentage); };

        // Subscribe to completion
        task.DownloadFileCompleted += (_, _) => { onCompleted(task); };

        // Subscribe to errors
        task.ErrorOccurred += (_, e) => { onError(task, e.GetException()); };

        // Start download
        Logger.LogInformation("Starting download: {Url}", task.Url);
        _ = task.StartAsync(httpClient);

        return task;
    }
}