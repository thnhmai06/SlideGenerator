using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Features.Downloads;
using SlideGenerator.Domain.Features.Downloads;
using SlideGenerator.Infrastructure.Common.Base;
using SlideGenerator.Infrastructure.Features.Downloads.Models;

namespace SlideGenerator.Infrastructure.Features.Downloads.Services;

/// <summary>
///     Download service implementation using Downloader library.
/// </summary>
public class DownloadService(ILogger<DownloadService> logger, ILoggerFactory? loggerFactory = null)
    : Service(logger), IDownloadService, IDownloadClient
{
    public async Task<DownloadResult> DownloadAsync(Uri uri, DirectoryInfo saveFolder,
        CancellationToken cancellationToken)
    {
        var task = CreateImageTask(uri.ToString(), saveFolder);
        var tcs = new TaskCompletionSource<DownloadResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        task.DownloadCompletedEvents += (_, args) =>
        {
            tcs.TrySetResult(args.Success
                ? new DownloadResult(true, args.FilePath, null)
                : new DownloadResult(false, args.FilePath, args.Error?.Message));
        };

        await using var registration = cancellationToken.Register(() =>
        {
            task.Cancel();
            tcs.TrySetCanceled(cancellationToken);
        });

        await DownloadTask(task);
        return await tcs.Task;
    }

    public IDownloadTask CreateImageTask(string url, DirectoryInfo saveFolder)
    {
        var task = new DownloadImageTask(url, saveFolder, loggerFactory);

        // Hook logging events
        task.DownloadStartedEvents += (_, args) =>
        {
            Logger.LogInformation("Downloading: {FilePath} ({Url})",
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