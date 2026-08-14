/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: App.axaml.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using SlideGenerator.Cloud;
using SlideGenerator.Desktop.Bootstrap;
using SlideGenerator.Desktop.Shell;
using SlideGenerator.Document;
using SlideGenerator.Generator;
using SlideGenerator.Image;
using SlideGenerator.Logging;
using SlideGenerator.Recipe;
using SlideGenerator.Settings;
using SlideGenerator.Settings.Immutable;
using SlideGenerator.Settings.Mutable;
using SlideGenerator.Summarizer;
using SlideGenerator.Utilities;

namespace SlideGenerator.Desktop;

/// <summary>
///     Avalonia application object. Builds the generic host (DI container for all domain modules),
///     starts it, then shows the (placeholder, for now) main window.
/// </summary>
public sealed class App : Application
{
    private IHost? _host;

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = NameAndPaths.BasePath
            });
            ConfigureServices(builder.Services);
            _host = builder.Build();

            _host.StartAsync().GetAwaiter().GetResult();
            InitializeAsync(_host.Services).GetAwaiter().GetResult();

            desktop.MainWindow = new MainWindow();
            desktop.ShutdownRequested += (_, _) => ShutdownAsync(_host).GetAwaiter().GetResult();

#if DEBUG
            this.AttachDeveloperTools();
#endif
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        Log.Information("Registering Foundation services...");
        services.AddTransient(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var level = cfg.GetValue("Logging:Workflow:MinimumLevel", LogEventLevel.Information);
            return new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails();
        });
        services.AddLoggingServices();
        services.AddSettingsServices();
        services.AddCloudServices();

        Log.Information("Registering Domain services...");
        services.AddDocumentServices();
        services.AddImageServices();
        services.AddRecipeServices();
        services.AddSummarizationServices();

        Log.Information("Registering Application services...");
        services.AddGeneratorServices();
        services.AddDesktopServices();
    }

    private static async Task InitializeAsync(IServiceProvider services)
    {
        Log.Information("Initializing application directories...");
        NameAndPaths.InitializeDirectories();
        Log.Information("Data DB: {Path}", NameAndPaths.DataFolder.DataFile.FilePath);

        var latestPath = Path.Combine(NameAndPaths.LogsFolder.SystemPath, "latest.log");
        var currentLogFiles = Directory.GetFiles(NameAndPaths.LogsFolder.SystemPath, "*.log")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (currentLogFiles is not null)
            try
            {
                HardLink.Create(latestPath, currentLogFiles);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Warning("Could not create 'latest.log' hard link: {Message}", ex.Message);
            }

        var settingManager = services.GetRequiredService<ISettingManager>();
        Log.Information("Loading settings...");
        await settingManager.Load().ConfigureAwait(false);

        var service = services.GetRequiredService<IService>();
        Log.Information("Starting job runner...");
        await service.InitializeAsync().ConfigureAwait(false);

        await UpdateChecker.CheckForUpdatesAsync().ConfigureAwait(false);

        Log.Information("Setup completed!");
    }

    private static async Task ShutdownAsync(IHost host)
    {
        try
        {
            var service = host.Services.GetRequiredService<IService>();
            await service.ShutdownAsync().ConfigureAwait(false);

            var settingManager = host.Services.GetRequiredService<ISettingManager>();
            await settingManager.Save().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during shutdown.");
        }
        finally
        {
            await host.StopAsync().ConfigureAwait(false);
        }
    }
}