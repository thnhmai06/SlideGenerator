using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using TaoSlideTotNghiep.DTOs;
using TaoSlideTotNghiep.Logic;
using TaoSlideTotNghiep.Utils;

namespace TaoSlideTotNghiep.Hubs;

/// <summary>
/// SignalR Hub for download operations.
/// </summary>
public class DownloadHub(ILogger<DownloadHub> logger, IHttpClientFactory httpClientFactory)
    : Hub
{
    // Thread-safe storage for active downloads per connection
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ImageDownloadTask>> ConnectionDownloads = new();

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Download client connected: {ConnectionId}", Context.ConnectionId);
        ConnectionDownloads[Context.ConnectionId] = new ConcurrentDictionary<string, ImageDownloadTask>();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Download client disconnected: {ConnectionId}", Context.ConnectionId);
        
        // Cleanup active downloads for this connection
        if (ConnectionDownloads.TryRemove(Context.ConnectionId, out var downloads))
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

    private ConcurrentDictionary<string, ImageDownloadTask> GetDownloads()
    {
        return ConnectionDownloads.GetValueOrDefault(Context.ConnectionId) 
               ?? throw new InvalidOperationException("Connection not found");
    }

    /// <summary>
    /// Starts a new download.
    /// </summary>
    public async Task StartDownload(string url, string savePath)
    {
        var downloads = GetDownloads();
        var taskId = Guid.NewGuid().ToString();

        try
        {
            var httpClient = httpClientFactory.CreateClient();
            var correctedUrl = await HttpUtils.CorrectImageUrl(url, httpClient);
            
            var task = new ImageDownloadTask(correctedUrl, savePath, httpClient);
            downloads[taskId] = task;

            // Subscribe to progress updates
            task.ProgressChanged += progress =>
            {
                _ = Clients.Caller.SendAsync("DownloadProgress", new DownloadProgressResponse
                {
                    Url = url,
                    SavePath = savePath,
                    Progress = progress,
                    DownloadedBytes = task.DownloadedSize,
                    TotalBytes = task.TotalSize,
                    Status = task.Status.ToString()
                });
            };

            task.StatusChanged += status =>
            {
                _ = Clients.Caller.SendAsync("DownloadStatus", new
                {
                    TaskId = taskId,
                    Url = url,
                    SavePath = savePath,
                    Status = status.ToString()
                });
            };

            task.ErrorOccurred += ex =>
            {
                _ = Clients.Caller.SendAsync("DownloadError", new ErrorResponse(ex, RequestType.Download));
            };

            await Clients.Caller.SendAsync("DownloadStarted", new
            {
                TaskId = taskId,
                Url = url,
                SavePath = savePath
            });

            // Start download (fire and forget, progress will be reported via events)
            _ = task.StartAsync(httpClient);
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
        var downloads = GetDownloads();
        
        if (downloads.TryGetValue(taskId, out var task))
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
        var downloads = GetDownloads();
        
        if (downloads.TryGetValue(taskId, out var task))
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
        var downloads = GetDownloads();
        
        if (downloads.TryRemove(taskId, out var task))
        {
            task.Stop();
            task.Dispose();
            await Clients.Caller.SendAsync("DownloadStopped", taskId);
        }
    }

    /// <summary>
    /// Gets status of all downloads.
    /// </summary>
    public async Task GetDownloadStatuses()
    {
        var downloads = GetDownloads();
        var statuses = new List<object>();

        foreach (var taskId in downloads.Keys)
        {
            if (downloads.TryGetValue(taskId, out var task))
            {
                statuses.Add(new
                {
                    TaskId = taskId,
                    Url = task.Url,
                    SavePath = task.SavePath,
                    Status = task.Status.ToString(),
                    Progress = task.Progress,
                    DownloadedBytes = task.DownloadedSize,
                    TotalBytes = task.TotalSize,
                    IsPaused = task.IsPaused
                });
            }
        }

        await Clients.Caller.SendAsync("DownloadStatuses", statuses);
    }
}
