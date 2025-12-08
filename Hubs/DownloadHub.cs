using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using TaoSlideTotNghiep.DTOs;
using TaoSlideTotNghiep.Exceptions;
using TaoSlideTotNghiep.Models;
using TaoSlideTotNghiep.Services;
using TaoSlideTotNghiep.Utils;

namespace TaoSlideTotNghiep.Hubs;

/// <summary>
/// SignalR Hub for download operations.
/// </summary>
public class DownloadHub(IDownloadService downloadService, ILogger<DownloadHub> logger, IHttpClientFactory httpClientFactory)
    : Hub
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ImageDownloadTask>> DownloadTasksOfConnections = new();

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Download client connected: {ConnectionId}", Context.ConnectionId);
        DownloadTasksOfConnections[Context.ConnectionId] = new ConcurrentDictionary<string, ImageDownloadTask>();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Download client disconnected: {ConnectionId}", Context.ConnectionId);

        if (DownloadTasksOfConnections.TryRemove(Context.ConnectionId, out var downloads))
        {
            foreach (var key in downloads.Keys)
            {
                if (downloads.TryRemove(key, out var task))
                {
                    task.Stop();
                    task.Dispose();
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private ConcurrentDictionary<string, ImageDownloadTask> Downloads
        => DownloadTasksOfConnections.GetValueOrDefault(Context.ConnectionId)
           ?? throw new ConnectionNotFoundException(Context.ConnectionId);

    /// <summary>
    /// Starts a new download.
    /// </summary>
    public async Task StartDownload(string url, string savePath)
    {
        var taskId = Guid.NewGuid().ToString();

        try
        {
            var httpClient = httpClientFactory.CreateClient();
            var correctedUrl = await HttpUtils.CorrectImageUrlAsync(url, httpClient);

            var task = downloadService.CreateDownloadTask(correctedUrl, savePath, httpClient);
            Downloads[taskId] = task;

            // Subscribe to progress updates
            task.DownloadProgressChanged += (_, e) =>
            {
                Clients.Caller.SendAsync("DownloadProgress", new DownloadProgressResponse
                {
                    Url = url,
                    SavePath = savePath,
                    Progress = e.ProgressPercentage,
                    DownloadedBytes = task.DownloadedSize,
                    TotalBytes = task.TotalSize,
                    Status = task.Status.ToString()
                });
            };

            task.DownloadFileCompleted += (_, _) =>
            {
                Clients.Caller.SendAsync("DownloadStatus", new
                {
                    TaskId = taskId,
                    Url = url,
                    SavePath = savePath,
                    Status = task.Status.ToString()
                });
            };

            task.ErrorOccurred += (_, e) =>
            {
                Clients.Caller.SendAsync("DownloadError", new ErrorResponse(e.GetException(), RequestType.Download));
            };

            await Clients.Caller.SendAsync("DownloadStarted", new
            {
                TaskId = taskId,
                Url = url,
                SavePath = savePath
            });

            // Start download
            _ = downloadService.StartDownloadAsync(task, httpClient);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting download for {Url}", url);
            await Clients.Caller.SendAsync("ReceiveResponse", new ErrorResponse(ex, RequestType.Download));
        }
    }

    /// <summary>
    /// Pauses a download.
    /// </summary>
    public async Task PauseDownload(string taskId)
    {
        if (Downloads.TryGetValue(taskId, out var task))
        {
            task.Pause();
            await Clients.Caller.SendAsync("DownloadPaused", taskId);
        }
    }

    /// <summary>
    /// Resumes a paused download.
    /// </summary>
    public async Task ResumeDownload(string taskId)
    {
        if (Downloads.TryGetValue(taskId, out var task))
        {
            task.Resume();
            await Clients.Caller.SendAsync("DownloadResumed", taskId);
        }
    }

    /// <summary>
    /// Stops a download.
    /// </summary>
    public async Task StopDownload(string taskId)
    {
        if (Downloads.TryRemove(taskId, out var task))
        {
            task.Stop();
            task.Dispose();
            await Clients.Caller.SendAsync("DownloadStopped", taskId);
        }
    }

    /// <summary>
    /// Gets status of a download.
    /// </summary>
    public async Task DownloadStatus(string taskId)
    {
        if (Downloads.TryGetValue(taskId, out var task))
        {
            var status = new
            {
                TaskId = taskId,
                Url = task.Url,
                SavePath = task.SavePath,
                Status = task.Status.ToString(),
                Progress = task.Progress,
                DownloadedBytes = task.DownloadedSize,
                TotalBytes = task.TotalSize,
                IsPaused = task.IsPaused
            };
            await Clients.Caller.SendAsync("DownloadStatus", status);
        }
    }
}
