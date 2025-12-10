using System.Collections.Concurrent;
using System.Text.Json;
using Application.Contracts;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Exceptions;
using Domain.Models;
using Infrastructure.Exceptions.Services;
using Presentation.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs;

/// <summary>
/// SignalR Hub for download operations.
/// </summary>
public class DownloadHub(
    IDownloadService downloadService,
    ILogger<DownloadHub> logger,
    IHttpClientFactory httpClientFactory)
    : Hub
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ImageDownloadTask>>
        DownloadTasksOfConnections = new();

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("[Download] Client connected: {ConnectionId}", Context.ConnectionId);
        DownloadTasksOfConnections[Context.ConnectionId] = new ConcurrentDictionary<string, ImageDownloadTask>();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("[Download] Client disconnected: {ConnectionId}", Context.ConnectionId);

        if (DownloadTasksOfConnections.TryRemove(Context.ConnectionId, out var downloads))
            foreach (var key in downloads.Keys)
                if (downloads.TryRemove(key, out var task))
                {
                    task.Stop();
                    task.Dispose();
                }

        await base.OnDisconnectedAsync(exception);
    }

    private ConcurrentDictionary<string, ImageDownloadTask> Downloads
        => DownloadTasksOfConnections.GetValueOrDefault(Context.ConnectionId)
           ?? throw new ConnectionNotFoundException(Context.ConnectionId);

    /// <summary>
    /// Processes a download request based on type.
    /// </summary>
    public async Task ProcessRequest(JsonElement message)
    {
        Response response;
        var filePath = string.Empty;

        try
        {
            var typeStr = message.GetProperty("type").GetString()?.ToLowerInvariant();

            if (string.IsNullOrEmpty(typeStr))
                throw new TypeNotIncludedException(typeof(DownloadRequestType));

            filePath = message.GetProperty("filePath").GetString() ?? string.Empty;

            response = typeStr switch
            {
                "start" => await ExecuteStartAsync(
                    JsonSerializer.Deserialize<StartDownloadRequest>(message.GetRawText(), SerializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(StartDownloadRequest))),
                "pause" => ExecutePause(
                    JsonSerializer.Deserialize<PauseDownloadRequest>(message.GetRawText(), SerializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(PauseDownloadRequest))),
                "resume" => ExecuteResume(
                    JsonSerializer.Deserialize<ResumeDownloadRequest>(message.GetRawText(), SerializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(ResumeDownloadRequest))),
                "stop" => ExecuteStop(
                    JsonSerializer.Deserialize<StopDownloadRequest>(message.GetRawText(), SerializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(StopDownloadRequest))),
                "status" => ExecuteStatus(
                    JsonSerializer.Deserialize<StatusDownloadRequest>(message.GetRawText(), SerializerOptions)
                    ?? throw new InvalidRequestFormatException(nameof(StatusDownloadRequest))),
                _ => throw new TypeNotIncludedException(typeof(DownloadRequestType))
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing download request");
            response = new DownloadError(filePath, ex);
        }

        await Clients.Caller.SendAsync("ReceiveResponse", response);
    }

    private async Task<DownloadStartSuccess> ExecuteStartAsync(StartDownloadRequest request)
    {
        var httpClient = httpClientFactory.CreateClient();

        var task = await downloadService.CreateAndStartDownloadAsync(
            request.Url,
            request.FilePath,
            httpClient,
            onProgress: (t, progress) =>
            {
                Clients.Caller.SendAsync("ReceiveResponse", new DownloadStatusSuccess
                (
                    request.Url,
                    request.FilePath,
                    progress,
                    t.DownloadedSize,
                    t.TotalSize,
                    t.Status
                ));
            },
            onCompleted: t =>
            {
                Clients.Caller.SendAsync("ReceiveResponse", new DownloadStatusSuccess
                (
                    request.Url,
                    request.FilePath,
                    100,
                    t.DownloadedSize,
                    t.TotalSize,
                    t.Status
                ));
            },
            onError: (_, ex) =>
            {
                Clients.Caller.SendAsync("ReceiveResponse", new DownloadError(request.FilePath, ex));
            });

        Downloads[request.FilePath] = task;

        return new DownloadStartSuccess(request.FilePath);
    }

    private DownloadPauseSuccess ExecutePause(PauseDownloadRequest request)
    {
        if (Downloads.TryGetValue(request.FilePath, out var task)) task.Pause();

        return new DownloadPauseSuccess(request.FilePath);
    }

    private DownloadResumeSuccess ExecuteResume(ResumeDownloadRequest request)
    {
        if (Downloads.TryGetValue(request.FilePath, out var task)) task.Resume();

        return new DownloadResumeSuccess(request.FilePath);
    }

    private DownloadStopSuccess ExecuteStop(StopDownloadRequest request)
    {
        if (Downloads.TryRemove(request.FilePath, out var task))
        {
            task.Stop();
            task.Dispose();
        }

        return new DownloadStopSuccess(request.FilePath);
    }

    private DownloadStatusSuccess ExecuteStatus(StatusDownloadRequest request)
    {
        if (Downloads.TryGetValue(request.FilePath, out var task))
            return new DownloadStatusSuccess
            (
                task.Url,
                request.FilePath,
                task.Progress,
                task.DownloadedSize,
                task.TotalSize,
                task.Status
            );

        throw new DownloadTaskNotFoundException(request.FilePath);
    }
}