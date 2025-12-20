using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Download.Contracts;
using SlideGenerator.Domain.Download.Interfaces;
using SlideGenerator.Infrastructure.Base;
using SlideGenerator.Infrastructure.Download.Models;

namespace SlideGenerator.Infrastructure.Download.Services;

/// <summary>
///     Download service implementation using Downloader library.
/// </summary>
public class DownloadService(ILogger<DownloadService> logger, ILoggerFactory? loggerFactory = null)
    : Service(logger), IDownloadService
{
    public IDownloadTask CreateImageTask(string url, DirectoryInfo saveFolder)
    {
        var task = new DownloadImageTask(url, saveFolder, loggerFactory);

        // Hook logging events
        task.DownloadStartedEvents += (_, args) =>
        {
            Logger.LogInformation("Downloading: {FilePath} | {Url})",
                args.FilePath, args.Url);
        };
        task.DownloadProgressedEvents += (_, args) =>
        {
            Logger.LogTrace("Progress: {FilePath} | {Downloaded}/{Total} ({Percent}%)",
                task.FilePath, args.BytesReceived, args.TotalBytes, args.ProgressPercentage);
        };
        task.DownloadCompletedEvents += (_, args) =>
        {
            if (args.Success)
                Logger.LogInformation("Completed: {FilePath}", args.FilePath);
            else if (args.Error != null)
                Logger.LogWarning("Failed: {FilePath} | {ExceptionType}: {ExceptionMsg}",
                    args.FilePath, args.Error?.GetType(), args.Error?.Message);
        };

        return task;
    }

    public async Task DownloadTask(IDownloadTask task)
    {
        await task.DownloadFileAsync();
    }
}