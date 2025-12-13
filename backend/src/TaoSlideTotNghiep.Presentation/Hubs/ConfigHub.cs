using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using TaoSlideTotNghiep.Application.Base.DTOs.Responses;
using TaoSlideTotNghiep.Application.Configs;
using TaoSlideTotNghiep.Application.Configs.DTOs.Components;
using TaoSlideTotNghiep.Application.Configs.DTOs.Requests;
using TaoSlideTotNghiep.Application.Configs.DTOs.Responses.Errors;
using TaoSlideTotNghiep.Application.Configs.DTOs.Responses.Successes;
using TaoSlideTotNghiep.Application.Configs.Models;
using TaoSlideTotNghiep.Application.Job.Contracts;
using TaoSlideTotNghiep.Infrastructure.Configs;
using TaoSlideTotNghiep.Presentation.Exceptions.Hubs;

namespace TaoSlideTotNghiep.Presentation.Hubs;

public class ConfigHub(
    IJobManager jobManager,
    ILogger<ConfigHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("[Config] Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("[Config] Client disconnected: {ConnectionId}", Context.ConnectionId);
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
               ?? throw new InvalidRequestFormatException(typeof(T).Name);
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
            new JobConfig(
                config.Job.MaxConcurrentJobs,
                config.Job.OutputFolder,
                config.Job.HangfireDbPath));
    }

    private ConfigUpdateSuccess ExecuteUpdateConfig(ConfigUpdate request)
    {
        if (jobManager.HasActiveJobs())
            throw new InvalidOperationException("Cannot update config while jobs are running. Cancel all jobs first.");

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
                    MaxConcurrentJobs = request.Job.MaxConcurrentJobs,
                    OutputFolder = request.Job.OutputFolder,
                    HangfireDbPath = request.Job.HangfireDbPath
                }
                : ConfigHolder.Value.Job
        };
        ConfigHolder.Value = config;
        ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker);

        logger.LogInformation("Configuration updated by client {ConnectionId}", Context.ConnectionId);
        return new ConfigUpdateSuccess(true, "Configuration updated successfully");
    }

    private ConfigReloadSuccess ExecuteReloadConfig()
    {
        if (jobManager.HasActiveJobs())
            throw new InvalidOperationException("Cannot reload config while jobs are running.");

        var loaded = ConfigLoader.Load(ConfigHolder.Locker);
        if (loaded != null)
            ConfigHolder.Value = loaded;
        logger.LogInformation("Configuration reloaded by client {ConnectionId}", Context.ConnectionId);

        return new ConfigReloadSuccess(true, "Configuration reloaded successfully");
    }

    private ConfigResetSuccess ExecuteResetConfig()
    {
        if (jobManager.HasActiveJobs())
            throw new InvalidOperationException("Cannot reset config while jobs are running.");

        ConfigHolder.Reset();
        ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker);
        logger.LogInformation("Configuration reset to defaults by client {ConnectionId}", Context.ConnectionId);

        return new ConfigResetSuccess(true, "Configuration reset to defaults");
    }
}