using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Download.Contracts;
using SlideGenerator.Domain.Download.Interfaces;
using SlideGenerator.Framework.Cloud;
using SlideGenerator.Infrastructure.Engines.Download.Models;
using SlideGenerator.Infrastructure.Services.Base;

namespace SlideGenerator.Infrastructure.Services.Download;

/// <summary>
/// Download service implementation using Downloader library.
/// </summary>
public class DownloadService(ILogger<DownloadService> logger) : Service(logger),
    IDownloadService
{
    /// <inheritdoc />
    public async Task<IDownloadTask> CreateAndStartDownloadAsync(
        string url,
        string savePath,
        HttpClient httpClient,
        Action<IDownloadTask, double> onProgress,
        Action<IDownloadTask> onCompleted,
        Action<IDownloadTask, Exception> onError)
    {
        Logger.LogInformation("Creating download task: {Url} -> {SavePath}", url, savePath);

        // Correct the URL if needed (Google Drive, OneDrive, etc.)
        var correctedUrl = await CloudUrlResolver.ResolveAsync(url, httpClient);

        var task = new ImageDownloadTask(correctedUrl, savePath);

        // Subscribe to progress updates
        task.ProgressChanged += (_, e) => onProgress(task, e.ProgressPercentage);

        // Subscribe to completion
        task.Completed += (_, e) =>
        {
            if (e.Success)
                onCompleted(task);
            else if (e.Error != null)
                onError(task, e.Error);
        };

        // Subscribe to errors
        task.ErrorOccurred += (_, ex) => onError(task, ex);

        // Start download
        Logger.LogInformation("Starting download: {Url}", task.Url);
        _ = task.StartAsync();

        return task;
    }
}