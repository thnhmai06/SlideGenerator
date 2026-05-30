/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Ipc
 * File: Program.Startup.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Ipc.Infrastructure;
using SlideGenerator.Settings.Domain.Rules;
using SlideGenerator.Utilities;
using StreamJsonRpc;

namespace SlideGenerator.Ipc;

internal static partial class Program
{
    /// <summary>
    ///     Performs the ordered startup sequence, then blocks on <see cref="JsonRpc.Completion" />
    ///     until the client closes the connection. Tears down all resources in reverse order on exit.
    /// </summary>
    private static async Task StartupAsync(IHost host, JsonSerializerOptions jsonOptions)
    {
        var services = host.Services;
        var logger = _systemLogger!;
        var workflowService = services.GetRequiredService<IGeneratingService>();
        JsonRpc? jsonRpc = null;

        try
        {
            if (_logFilePath is not null)
            {
                var latestPath = Path.Combine(Path.GetDirectoryName(_logFilePath)!, "latest.log");
                try
                {
                    HardLink.Create(latestPath, _logFilePath);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger.LogWarning("Could not create 'latest.log' hard link: {Message}", ex.Message);
                }

                logger.LogInformation("System logger initialized. Current log file: {Path}", _logFilePath);
            }

            logger.LogInformation("Initializing application directories...");
            NameAndPaths.InitializeDirectories();

            logger.LogInformation("Loading settings...");
            await LoadSettingsAsync(services).ConfigureAwait(false);

            logger.LogInformation("Starting workflow host...");
            await workflowService.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            logger.LogInformation("Initializing JSON-RPC connection...");
            jsonRpc = JsonRpcBootstrap.Create(services, jsonOptions);
            JsonRpcBootstrap.AttachProgressObserver(services, jsonRpc);

            logger.LogInformation("Setup completed! Application is listening.");

            var lifetime = services.GetRequiredService<IHostApplicationLifetime>();
            await Task.WhenAny(jsonRpc.Completion, Task.Delay(-1, lifetime.ApplicationStopping))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during IPC lifecycle.");
#if DEBUG
            throw;
#endif
        }
        finally
        {
            await TeardownAsync(host, services, workflowService, jsonRpc).ConfigureAwait(false);
        }
    }

    /// <summary>Shuts down all components in reverse startup order.</summary>
    private static async Task TeardownAsync(
        IHost host,
        IServiceProvider services,
        IGeneratingService generatingService,
        JsonRpc? jsonRpc)
    {
        var logger = _systemLogger!;
        try
        {
            var eventBus = services.GetRequiredService<GeneratingEventBus>();
            services.GetRequiredService<WorkflowProgressObserver>().Detach(eventBus);
            jsonRpc?.Dispose();
            await generatingService.ShutdownAsync(CancellationToken.None).ConfigureAwait(false);
            await SaveSettingsAsync(services).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during TeardownAsync.");
        }
        finally
        {
            await host.StopAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Prints the ASCII art banner and build metadata to the system log.</summary>
    private static void PrintWelcomeMessage()
    {
        _systemLogger?.LogInformation("\n{AsciiArt}", WelcomeMessages.Name);
        _systemLogger?.LogInformation(WelcomeMessages.Line);
        _systemLogger?.LogInformation(WelcomeMessages.Version);
        _systemLogger?.LogInformation(WelcomeMessages.Description);
        _systemLogger?.LogInformation(WelcomeMessages.Line);
        _systemLogger?.LogInformation(WelcomeMessages.License);
        _systemLogger?.LogInformation(WelcomeMessages.RepositoryUrl);
        _systemLogger?.LogInformation(WelcomeMessages.Line);
    }

    /// <summary>Registers the Ctrl+C handler to trigger a graceful host shutdown.</summary>
    private static void RegisterCtrlCHandler(IHost host)
    {
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _systemLogger?.LogWarning("Ctrl+C detected. Initiating graceful shutdown...");
            lifetime.StopApplication();
        };
    }

    /// <summary>Registers process-wide unhandled exception and task exception handlers.</summary>
    private static void RegisterExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                _systemLogger?.LogCritical(ex, "Unhandled AppDomain exception. IsTerminating: {IsTerminating}",
                    e.IsTerminating);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _systemLogger?.LogCritical(e.Exception, "Unobserved Task exception.");
#if !DEBUG
            e.SetObserved();
#endif
        };

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            _systemLogger?.LogInformation("Process exiting, flushing logs...");
            Log.CloseAndFlush();
        };
    }
}