/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Ipc
 * File: Program.cs
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

using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlideGenerator.Cloud.Injection;
using SlideGenerator.Coordinator.Injection;
using SlideGenerator.Cryptography.Injection;
using SlideGenerator.Document.Injection;
using SlideGenerator.Download.Injection;
using SlideGenerator.Generating;
using SlideGenerator.Generating.Application.Abstractions;
using SlideGenerator.Image.Injection;
using SlideGenerator.Ipc.Handlers;
using SlideGenerator.Ipc.Infrastructure;
using SlideGenerator.Ipc.Infrastructure.Adapters;
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Logging.Injection;
using SlideGenerator.Scanning.Injection;
using SlideGenerator.Settings.Application.Abstractions;
using SlideGenerator.Settings.Domain.Rules;
using SlideGenerator.Settings.Injection;
using StreamJsonRpc;

namespace SlideGenerator.Ipc;

/// <summary>
///     Application entry point for the JSON-RPC 2.0 IPC sidecar.
///     Bootstraps the host, wires all services and method routes via StreamJsonRpc,
///     then blocks until the client closes the connection.
/// </summary>
internal static class Program
{
    public static readonly DateTime StartupTime = DateTime.UtcNow;
    private static ISystemLogger? _systemLogger;

    /// <summary>Application entry point.</summary>
    /// <param name="args">Command-line arguments passed by the Tauri sidecar launcher.</param>
    public static async Task<int> Main(string[] args)
    {
        ConfigureEncoding();

        var bootstrapConfiguration = new ConfigurationBuilder()
            .SetBasePath(NameAndPaths.BasePath)
            .AddJsonFile("appsettings.json", true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        _systemLogger = SystemLoggerBootstrapper.Initialize(NameAndPaths.LogsFolder.System, bootstrapConfiguration);

        PrintWelcomeMessage();

        try
        {
            _systemLogger.Information("Application starting...");

            // Set up global exception logging and prevent crashes where possible
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    _systemLogger?.Fatal(ex, "Unhandled AppDomain exception. IsTerminating: {IsTerminating}",
                        e.IsTerminating);
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                _systemLogger?.Fatal(e.Exception, "Unobserved Task exception.");
#if !DEBUG
                e.SetObserved();
#endif
            };

            // Ensure logs are flushed on unexpected process exit
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                _systemLogger?.Information("Process exiting, flushing logs...");
                SystemLoggerBootstrapper.Flush();
            };

            var builder = Host.CreateApplicationBuilder(args);
            ConfigureServices(builder.Services, builder.Configuration, _systemLogger);

            using var host = builder.Build();

            // Intercept Ctrl+C to trigger a clean shutdown via the host lifetime
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // Prevent immediate termination
                _systemLogger.Warning("Ctrl+C detected. Initiating graceful shutdown...");
                lifetime.StopApplication();
            };

            await host.StartAsync();

            var jsonOptions = BuildJsonSerializerOptions();
            await StartupAsync(host, jsonOptions);

            await host.StopAsync();
        }
        catch (Exception ex)
        {
            _systemLogger.Fatal(ex, "Fatal exception in Main");
#if DEBUG
            throw;
#else
            return 1;
#endif
        }
        finally
        {
            await SystemLoggerBootstrapper.FlushAsync().ConfigureAwait(false);
        }

        return 0;
    }

    #region Encoding

    /// <summary>
    ///     Configures stderr to use UTF-8 so Serilog log output is transmitted correctly
    ///     regardless of the platform's default code page.
    ///     Stdin and stdout are owned by StreamJsonRpc which writes raw UTF-8 bytes directly,
    ///     bypassing <see cref="Console" /> encoding wrappers.
    /// </summary>
    private static void ConfigureEncoding()
    {
        Console.SetError(new StreamWriter(
            Console.OpenStandardError(),
            new UTF8Encoding(false),
            leaveOpen: true) { AutoFlush = true });
    }

    #endregion

    #region DI Registration

    /// <summary>
    ///     Registers all services into the DI container. Delegates to per-module
    ///     <c>Registration.cs</c> extension methods following the project convention.
    ///     The <see cref="JsonRpc" /> connection is not registered in the container
    ///     because it requires stream access available only after host construction.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <param name="configuration">The application configuration provider.</param>
    /// <param name="systemLogger">The system logger instance for startup logging.</param>
    private static void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        ISystemLogger? systemLogger)
    {
        systemLogger?.Information("Registering core services...");
        services.AddLoggingModule(configuration);
        if (systemLogger is not null) services.AddSystemLogging(systemLogger);
        services.AddCryptographyServices();

        systemLogger?.Information("Registering domain modules...");
        services.AddSettingServices();
        services.AddDocumentServices(systemLogger);
        services.AddCoordinatorServices();
        services.AddCloudServices();
        services.AddDownloadServices();
        services.AddImageServices();
        services.AddGeneratingServices();
        services.AddScanningServices();

        systemLogger?.Information("Registering workflow and IPC infrastructure...");
        services.AddWorkflow(x => x.UseSqlite(NameAndPaths.WorkflowsFile.ConnectionString, true));
        services.AddIpcServices();
    }

    #endregion

    #region Startup & Run

    /// <summary>
    ///     Performs the ordered startup sequence, then blocks on <see cref="JsonRpc.Completion" />
    ///     until the client closes the connection. Tears down all resources in reverse order on exit.
    /// </summary>
    /// <param name="host">The fully built application host.</param>
    /// <param name="jsonOptions">Serializer options forwarded to the StreamJsonRpc formatter.</param>
    private static async Task StartupAsync(IHost host, JsonSerializerOptions jsonOptions)
    {
        var services = host.Services;
        var logger = services.GetRequiredService<ISystemLogger>();
        var workflowService = services.GetRequiredService<IGeneratingService>();
        JsonRpc? jsonRpc = null;

        try
        {
            logger.Information("Initializing application directories...");
            NameAndPaths.InitializeDirectories();

            logger.Information("Loading settings...");
            await LoadSettingsAsync(services);

            logger.Information("Starting workflow host...");
            await workflowService.InitializeAsync(CancellationToken.None);

            logger.Information("Initializing JSON-RPC connection...");
            jsonRpc = CreateAndConfigureJsonRpc(services, jsonOptions);
            AttachProgressObserver(services, jsonRpc);

            logger.Information("Setup completed! Application is listening.");

            // Wait for either the RPC connection to close OR the host to signal shutdown (e.g., via Ctrl+C)
            var lifetime = services.GetRequiredService<IHostApplicationLifetime>();
            await Task.WhenAny(jsonRpc.Completion, Task.Delay(-1, lifetime.ApplicationStopping));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Exception occurred during IPC lifecycle.");
#if DEBUG
            throw;
#endif
        }
        finally
        {
            await TeardownAsync(host, services, workflowService, jsonRpc);
        }
    }

    /// <summary>
    ///     Prints the application welcome message and metadata from <see cref="WelcomeMessages" /> using the logger.
    /// </summary>
    private static void PrintWelcomeMessage()
    {
        _systemLogger?.Information("\n{AsciiArt}", WelcomeMessages.Name);
        _systemLogger?.Information(WelcomeMessages.Line);
        _systemLogger?.Information(WelcomeMessages.Version);
        _systemLogger?.Information(WelcomeMessages.Description);
        _systemLogger?.Information(WelcomeMessages.Line);
        _systemLogger?.Information(WelcomeMessages.License);
        _systemLogger?.Information(WelcomeMessages.RepositoryUrl);
        _systemLogger?.Information(WelcomeMessages.Line);
    }

    /// <summary>
    ///     Creates the <see cref="JsonRpc" /> connection over stdin/stdout using NDJSON framing
    ///     and STJ serialization, then registers all method handlers by their JSON-RPC method names.
    ///     Calls <see cref="JsonRpc.StartListening" /> before returning.
    /// </summary>
    /// <param name="services">The application service provider.</param>
    /// <param name="jsonOptions">STJ options shared with the <see cref="SystemTextJsonFormatter" />.</param>
    /// <returns>The started <see cref="JsonRpc" /> instance.</returns>
    private static JsonRpc CreateAndConfigureJsonRpc(IServiceProvider services, JsonSerializerOptions jsonOptions)
    {
        var formatter = new SystemTextJsonFormatter { JsonSerializerOptions = jsonOptions };
        var handler = new NewLineDelimitedMessageHandler(
            Console.OpenStandardOutput(),
            Console.OpenStandardInput(),
            formatter);
        var jsonRpc = new JsonRpc(handler)
        {
            CancelLocallyInvokedMethodsWhenConnectionIsClosed = true
        };

        var generatingActiveHandler = services.GetRequiredService<GeneratingActiveHandler>();
        var generatingCompletedHandler = services.GetRequiredService<GeneratingCompletedHandler>();
        var scanningHandler = services.GetRequiredService<ScanningHandler>();
        var settingsHandler = services.GetRequiredService<SettingsHandler>();

        #region generating.active

        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.StartAsync)),
            generatingActiveHandler, Attr("generating.active.start"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.CancelAsync)),
            generatingActiveHandler, Attr("generating.active.cancel"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.PauseAsync)),
            generatingActiveHandler, Attr("generating.active.pause"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.ResumeAsync)),
            generatingActiveHandler, Attr("generating.active.resume"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.CancelAllAsync)),
            generatingActiveHandler, new JsonRpcMethodAttribute("generating.active.cancelAll"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.PauseAllAsync)),
            generatingActiveHandler, new JsonRpcMethodAttribute("generating.active.pauseAll"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.ListAsync)),
            generatingActiveHandler, new JsonRpcMethodAttribute("generating.active.list"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.QueryAsync)),
            generatingActiveHandler, Attr("generating.active.query"));

        #endregion

        #region generating.completed

        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingCompletedHandler>(nameof(GeneratingCompletedHandler.ListAsync)),
            generatingCompletedHandler, new JsonRpcMethodAttribute("generating.completed.list"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingCompletedHandler>(nameof(GeneratingCompletedHandler.QueryAsync)),
            generatingCompletedHandler, Attr("generating.completed.query"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingCompletedHandler>(nameof(GeneratingCompletedHandler.DeleteAsync)),
            generatingCompletedHandler, Attr("generating.completed.delete"));
        jsonRpc.AddLocalRpcMethod(
            GetMethod<GeneratingCompletedHandler>(nameof(GeneratingCompletedHandler.DeleteAllAsync)),
            generatingCompletedHandler, new JsonRpcMethodAttribute("generating.completed.deleteAll"));

        #endregion

        #region scanning

        jsonRpc.AddLocalRpcMethod(GetMethod<ScanningHandler>(nameof(ScanningHandler.ScanWorkbookAsync)),
            scanningHandler, Attr("scanning.scanWorkbook"));
        jsonRpc.AddLocalRpcMethod(GetMethod<ScanningHandler>(nameof(ScanningHandler.ScanPresentationAsync)),
            scanningHandler, Attr("scanning.scanPresentation"));

        #endregion

        #region settings

        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.GetAsync)), settingsHandler,
            new JsonRpcMethodAttribute("settings.get"));
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.UpdateAsync)), settingsHandler,
            Attr("settings.update"));
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.ResetToDefaultsAsync)),
            settingsHandler, new JsonRpcMethodAttribute("settings.resetToDefaults"));

        #endregion

        jsonRpc.StartListening();
        return jsonRpc;

        #region Helpers

        static JsonRpcMethodAttribute Attr(string name)
        {
            return new JsonRpcMethodAttribute(name) { UseSingleObjectParameterDeserialization = true };
        }

        static MethodInfo GetMethod<T>(string name)
        {
            return typeof(T).GetMethod(name) ??
                   throw new InvalidOperationException($"Method {name} not found on {typeof(T).Name}");
        }

        #endregion
    }

    /// <summary>
    ///     Attaches the <see cref="WorkflowProgressObserver" /> to the <see cref="GeneratingEventBus" />
    ///     so workflow lifecycle events are forwarded as <c>workflow/progress</c> notifications on stdout.
    /// </summary>
    /// <param name="services">The application service provider.</param>
    /// <param name="jsonRpc">The active JSON-RPC connection used to send notifications.</param>
    private static void AttachProgressObserver(IServiceProvider services, JsonRpc jsonRpc)
    {
        var eventBus = services.GetRequiredService<GeneratingEventBus>();
        var observer = services.GetRequiredService<WorkflowProgressObserver>();
        observer.Attach(eventBus, jsonRpc);
    }

    /// <summary>Shuts down all components in the reverse of startup order.</summary>
    private static async Task TeardownAsync(
        IHost host,
        IServiceProvider services,
        IGeneratingService generatingService,
        JsonRpc? jsonRpc)
    {
        var logger = services.GetRequiredService<ISystemLogger>();

        try
        {
            var eventBus = services.GetRequiredService<GeneratingEventBus>();
            services.GetRequiredService<WorkflowProgressObserver>().Detach(eventBus);

            jsonRpc?.Dispose();

            await generatingService.ShutdownAsync(CancellationToken.None);

            await SaveSettingsAsync(services);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during TeardownAsync.");
        }
        finally
        {
            await host.StopAsync();
        }
    }

    #endregion

    #region Helpers

    /// <summary>Loads persisted settings from the disk before any workflow is started.</summary>
    /// <param name="services">The application service provider.</param>
    private static async Task LoadSettingsAsync(IServiceProvider services)
    {
        var settingManager = services.GetRequiredService<ISettingManager>();
        await settingManager.Load();
    }

    /// <summary>Persists current settings to disk before the application shutdown.</summary>
    /// <param name="services">The application service provider.</param>
    private static async Task SaveSettingsAsync(IServiceProvider services)
    {
        var settingManager = services.GetRequiredService<ISettingManager>();
        await settingManager.Save();
    }

    /// <summary>
    ///     Builds the shared <see cref="JsonSerializerOptions" /> passed to the
    ///     <see cref="SystemTextJsonFormatter" /> for all JSON-RPC serialization.
    /// </summary>
    private static JsonSerializerOptions BuildJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new RoiOptionJsonAdapter(),
                new RectangleFJsonAdapter(),
                new JsonStringEnumConverter()
            }
        };
    }

    #endregion
}