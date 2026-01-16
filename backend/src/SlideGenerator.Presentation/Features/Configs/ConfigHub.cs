using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SlideGenerator.Application.Common.Base.DTOs.Responses;
using SlideGenerator.Application.Features.Configs;
using SlideGenerator.Application.Features.Configs.DTOs.Components;
using SlideGenerator.Application.Features.Configs.DTOs.Requests;
using SlideGenerator.Application.Features.Configs.DTOs.Responses.Errors;
using SlideGenerator.Application.Features.Configs.DTOs.Responses.Successes;
using SlideGenerator.Application.Features.Images;
using SlideGenerator.Application.Features.Jobs.Contracts;
using SlideGenerator.Domain.Configs;
using SlideGenerator.Domain.Features.Jobs.Enums;
using SlideGenerator.Infrastructure.Features.Configs;
using HubBase = SlideGenerator.Presentation.Common.Hubs.Hub;

namespace SlideGenerator.Presentation.Features.Configs;

/// <summary>
///     SignalR hub for configuration management.
/// </summary>
public class ConfigHub(
    IJobManager jobManager,
    IImageService imageService,
    ILogger<ConfigHub> logger) : HubBase
{
    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task ProcessRequest(JsonElement message)
    {
        Response response;

        try
        {
            var typeStr = message.GetProperty("type").GetString()?.ToLowerInvariant();

            response = typeStr switch
            {
                "get" => ExecuteGetConfig(),
                "update" => ExecuteUpdateConfig(Deserialize<ConfigUpdate>(message)),
                "reload" => ExecuteReloadConfig(),
                "reset" => ExecuteResetConfig(),
                "modelstatus" => ExecuteGetModelStatus(),
                "modelcontrol" => await ExecuteModelControlAsync(Deserialize<ModelControl>(message)),
                _ => throw new ArgumentOutOfRangeException(nameof(typeStr), typeStr, "Unknown config request type")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing config request");
            response = new ConfigError(ex);
        }

        await Clients.Caller.SendAsync("ReceiveResponse", response);
    }

    private ConfigGetSuccess ExecuteGetConfig()
    {
        var config = ConfigHolder.Value;

        return new ConfigGetSuccess(
            new ServerConfig(config.Server.Host, config.Server.Port, config.Server.Debug),
            new DownloadConfig(
                config.Download.MaxChunks,
                config.Download.LimitBytesPerSecond,
                config.Download.SaveFolder,
                new RetryConfig(config.Download.Retry.Timeout, config.Download.Retry.MaxRetries)),
            new JobConfig(config.Job.MaxConcurrentJobs),
            new ImageConfig(
                new FaceConfig(
                    config.Image.Face.Confidence,
                    config.Image.Face.UnionAll),
                new SaliencyConfig(
                    config.Image.Saliency.PaddingTop,
                    config.Image.Saliency.PaddingBottom,
                    config.Image.Saliency.PaddingLeft,
                    config.Image.Saliency.PaddingRight))
        );
    }

    private ConfigUpdateSuccess ExecuteUpdateConfig(ConfigUpdate request)
    {
        if (HasWorkingJobs())
            throw new InvalidOperationException(
                "Cannot update config while jobs are running. Pause or complete them first.");

        var config = new Config
        {
            Server = request.Server != null
                ? new Config.ServerConfig
                {
                    Host = request.Server.Host,
                    Debug = request.Server.Debug,
                    Port = request.Server.Port
                }
                : ConfigHolder.Value.Server,
            Download = request.Download != null
                ? new Config.DownloadConfig
                {
                    MaxChunks = request.Download.MaxChunks,
                    LimitBytesPerSecond = request.Download.LimitBytesPerSecond,
                    SaveFolder = request.Download.SaveFolder,
                    Retry = new Config.DownloadConfig.RetryConfig
                    {
                        Timeout = request.Download.Retry.Timeout,
                        MaxRetries = request.Download.Retry.MaxRetries
                    }
                }
                : ConfigHolder.Value.Download,
            Job = request.Job != null
                ? new Config.JobConfig
                {
                    MaxConcurrentJobs = request.Job.MaxConcurrentJobs
                }
                : ConfigHolder.Value.Job,
            Image = request.Image != null
                ? new Config.ImageConfig
                {
                    Face = new Config.ImageConfig.FaceConfig
                    {
                        Confidence = request.Image.Face.Confidence,
                        UnionAll = request.Image.Face.UnionAll
                    },
                    Saliency = new Config.ImageConfig.SaliencyConfig
                    {
                        PaddingTop = request.Image.Saliency.PaddingTop,
                        PaddingBottom = request.Image.Saliency.PaddingBottom,
                        PaddingLeft = request.Image.Saliency.PaddingLeft,
                        PaddingRight = request.Image.Saliency.PaddingRight
                    }
                }
                : ConfigHolder.Value.Image
        };
        ConfigHolder.Value = config;
        ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker);

        logger.LogInformation("Configuration updated by client {ConnectionId}", Context.ConnectionId);
        return new ConfigUpdateSuccess(true, "Configuration updated successfully");
    }

    private ConfigReloadSuccess ExecuteReloadConfig()
    {
        if (HasWorkingJobs())
            throw new InvalidOperationException("Cannot reload config while jobs are running.");

        var loaded = ConfigLoader.Load(ConfigHolder.Locker);
        if (loaded != null)
            ConfigHolder.Value = loaded;
        logger.LogInformation("Configuration reloaded by client {ConnectionId}", Context.ConnectionId);

        return new ConfigReloadSuccess(true, "Configuration reloaded successfully");
    }

    private ConfigResetSuccess ExecuteResetConfig()
    {
        if (HasWorkingJobs())
            throw new InvalidOperationException("Cannot reset config while jobs are running.");

        ConfigHolder.Reset();
        ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker);
        logger.LogInformation("Configuration reset to defaults by client {ConnectionId}", Context.ConnectionId);

        return new ConfigResetSuccess(true, "Configuration reset to defaults");
    }

    private ModelStatusSuccess ExecuteGetModelStatus()
    {
        return new ModelStatusSuccess(imageService.IsFaceModelAvailable);
    }

    private async Task<ModelControlSuccess> ExecuteModelControlAsync(ModelControl request)
    {
        var model = request.Model.ToLowerInvariant();
        var action = request.Action.ToLowerInvariant();

        if (model != "face")
            throw new ArgumentException($"Unknown model: {request.Model}");

        bool success;
        string message;

        switch (action)
        {
            case "init":
                if (HasWorkingJobs())
                    throw new InvalidOperationException("Cannot initialize model while jobs are running.");
                await imageService.InitFaceModelAsync();
                success = imageService.IsFaceModelAvailable;
                message = success
                    ? "Face detection model initialized successfully"
                    : "Failed to initialize face detection model";
                logger.LogInformation("Face model init by client {ConnectionId}: {Success}", Context.ConnectionId,
                    success);
                break;

            case "deinit":
                if (HasWorkingJobs())
                    throw new InvalidOperationException("Cannot deinitialize model while jobs are running.");
                await imageService.DeInitFaceModelAsync();
                success = !imageService.IsFaceModelAvailable;
                message = success
                    ? "Face detection model deinitialized successfully"
                    : "Failed to deinitialize face detection model";
                logger.LogInformation("Face model deinit by client {ConnectionId}: {Success}", Context.ConnectionId,
                    success);
                break;

            default:
                throw new ArgumentException($"Unknown action: {request.Action}");
        }

        return new ModelControlSuccess(request.Model, request.Action, success, message);
    }

    private bool HasWorkingJobs()
    {
        return jobManager.Active.EnumerateGroups()
            .Any(group => group.Status is GroupStatus.Pending or GroupStatus.Running);
    }
}