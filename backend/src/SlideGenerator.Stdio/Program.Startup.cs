/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: Program.Startup.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Settings.Domain.Rules;
using SlideGenerator.Stdio.Implementations;
using SlideGenerator.Utilities;
using StreamJsonRpc;

namespace SlideGenerator.Stdio;

internal static partial class Program
{
    /// <summary>
    ///     Performs the ordered startup sequence, then blocks on <see cref="JsonRpc.Completion" />
    ///     until the client closes the connection. Tears down all resources in reverse order on exit.
    /// </summary>
    private static async Task StartupAsync(IHost host, JsonSerializerOptions jsonOptions)
    {
        var services = host.Services;
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
                    Log.Warning("Could not create 'latest.log' hard link: {Message}", ex.Message);
                }

                Log.Information("System logger initialized. Current log file: {Path}", _logFilePath);
            }

            Log.Information("Initializing application directories...");
            NameAndPaths.InitializeDirectories();
            Log.Information("Workflows DB: {Path}", NameAndPaths.WorkflowsFile.FilePath);
            Log.Information("Recipes DB:   {Path}", NameAndPaths.RecipesFile.FilePath);

            Log.Information("Loading settings...");
            await LoadSettingsAsync(services).ConfigureAwait(false);

            Log.Information("Starting workflow host...");
            await workflowService.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            Log.Information("Initializing JSON-RPC connection...");
            jsonRpc = JsonRpcBootstrap.Create(services, jsonOptions);
            JsonRpcBootstrap.AttachProgressObserver(services, jsonRpc);

            Log.Information("Setup completed! Application is listening.");

            var lifetime = services.GetRequiredService<IHostApplicationLifetime>();
            await Task.WhenAny(jsonRpc.Completion, Task.Delay(-1, lifetime.ApplicationStopping))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception occurred during IPC lifecycle.");
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
            Log.Error(ex, "Error during TeardownAsync.");
        }
        finally
        {
            await host.StopAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Prints the ASCII art banner and build metadata to the system log.</summary>
    private static void PrintWelcomeMessage()
    {
        Log.Information("\n{AsciiArt}", Metadata.Name);
        Log.Information(Metadata.Line);
        Log.Information(Metadata.Version);
        Log.Information(Metadata.Description);
        Log.Information(Metadata.Line);
        Log.Information(Metadata.License);
        Log.Information(Metadata.RepositoryUrl);
        Log.Information(Metadata.Line);
    }

    /// <summary>Registers the Ctrl+C handler to trigger a graceful host shutdown.</summary>
    private static void RegisterCtrlCHandler(IHost host)
    {
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Log.Warning("Ctrl+C detected. Initiating graceful shutdown...");
            lifetime.StopApplication();
        };
    }

    /// <summary>Registers process-wide unhandled exception and task exception handlers.</summary>
    private static void RegisterExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Log.Fatal(ex, "Unhandled AppDomain exception. IsTerminating: {IsTerminating}",
                    e.IsTerminating);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Fatal(e.Exception, "Unobserved Task exception.");
#if !DEBUG
            e.SetObserved();
#endif
        };
    }
}