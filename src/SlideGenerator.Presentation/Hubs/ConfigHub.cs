using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Application.Configs;
using SlideGenerator.Application.Configs.DTOs.Components;
using SlideGenerator.Application.Configs.DTOs.Requests;
using SlideGenerator.Application.Configs.DTOs.Responses.Errors;
using SlideGenerator.Application.Configs.DTOs.Responses.Successes;
using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Domain.Configs;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Infrastructure.Configs;
using SlideGenerator.Presentation.Exceptions.Hubs;

namespace SlideGenerator.Presentation.Hubs;

public class ConfigHub(
    IJobManager jobManager,
    ILogger<ConfigHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

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

    private T Deserialize<T>(JsonElement message)
    {
        return JsonSerializer.Deserialize<T>(message.GetRawText(), SerializerOptions)
               ?? throw new InvalidRequestFormat(typeof(T).Name);
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
                    config.Image.Face.PaddingTop,
                    config.Image.Face.PaddingBottom,
                    config.Image.Face.PaddingLeft,
                    config.Image.Face.PaddingRight,
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
                        PaddingTop = request.Image.Face.PaddingTop,
                        PaddingBottom = request.Image.Face.PaddingBottom,
                        PaddingLeft = request.Image.Face.PaddingLeft,
                        PaddingRight = request.Image.Face.PaddingRight,
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

    private bool HasWorkingJobs()
    {
        return jobManager.Active.GetAllGroups()
            .Values
            .Any(group => group.Status is GroupStatus.Pending or GroupStatus.Running);
    }
}