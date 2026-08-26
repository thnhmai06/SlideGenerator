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

using System.Diagnostics;
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
using SlideGenerator.Desktop.Components;
using SlideGenerator.Desktop.Services.Localization;
using SlideGenerator.Desktop.Services.Progress;
using SlideGenerator.Desktop.Services.Theme;
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
///     Avalonia application object. Builds the generic host (DI container for all domain modules), shows the
///     main window immediately, then runs startup work asynchronously without blocking the UI thread.
/// </summary>
public sealed class App : Application
{
    /// <summary>
    ///     If startup work finishes within this window, the splash is skipped entirely — showing it only to
    ///     immediately replace it reads as a flash, not a screen (see the plan's Startup section).
    /// </summary>
    private static readonly TimeSpan SplashSkipThreshold = TimeSpan.FromMilliseconds(400);

    private IHost? _host;

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataTemplates.Add(new ViewLocator());
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

            var mainWindowViewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
            var window = new MainWindow { DataContext = mainWindowViewModel };
            desktop.MainWindow = window;
            desktop.ShutdownRequested += (_, _) => ShutdownAsync(_host).GetAwaiter().GetResult();

            // Fire-and-forget by design — OnFrameworkInitializationCompleted cannot be async, and awaiting
            // here would reintroduce the exact UI-thread block this rewrite removes. Every awaited step below
            // resumes back on the UI thread (Avalonia's SynchronizationContext), so DispatcherTimer-based
            // services (IProgressHub) constructed partway through remain UI-thread-affine. A fire-and-forget
            // Task's exception is otherwise swallowed silently (window would stay blank forever with no log
            // line at all) — catch and log explicitly instead of letting that happen.
            _ = StartupAsync(_host, mainWindowViewModel).ContinueWith(
                t => Log.Fatal(t.Exception, "Startup failed"),
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

            // Developer tools are attached via Program.cs's AppBuilder.WithDeveloperTools() instead of
            // this.AttachDeveloperTools() here — the two are the same underlying mechanism, and calling
            // both throws "Developer tools have already been attached."
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task StartupAsync(IHost host, MainWindowViewModel mainWindowViewModel)
    {
        // Every await in this method (and everything it calls) must stay on the UI thread — ConfigureAwait(true)
        // throughout, never (false) — because IProgressHub's DispatcherTimer is constructed partway through
        // and every Avalonia call after that point (theme, CurrentContent) needs UI-thread affinity.
        await host.StartAsync().ConfigureAwait(true);

        var sw = Stopwatch.StartNew();
        var initTask = InitializeAsync(host.Services);
        var wonRace = await Task.WhenAny(initTask, Task.Delay(SplashSkipThreshold)).ConfigureAwait(true) == initTask;

        if (!wonRace)
        {
            // Startup is taking a while — show the splash and let its lockup animation play in full, even if
            // init finishes before the animation would (an abrupt cut mid-transform looks broken).
            mainWindowViewModel.CurrentContent = host.Services.GetRequiredService<SplashViewModel>();
            await initTask.ConfigureAwait(true);
            // ApplyFromSettings() already ran inside InitializeAsync above, so MotionBrand already reflects
            // ReducedMotion — sizing the floor off BrandLockup's own total keeps the two in sync without
            // duplicating its hold-before/animate/hold-after math here.
            var motionBrand = ThemeService.GetMotionResource(Application.Current!, "MotionBrand");
            var minimumSplashDuration = BrandLockup.GetTotalDuration(motionBrand);
            var remaining = minimumSplashDuration - sw.Elapsed;
            if (remaining > TimeSpan.Zero) await Task.Delay(remaining).ConfigureAwait(true);
        }
        else
        {
            await initTask.ConfigureAwait(true); // propagate any exception; already resolved
        }

        mainWindowViewModel.CurrentContent = host.Services.GetRequiredService<ShellViewModel>();
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

        // Must resolve (construct + subscribe) before IService.InitializeAsync() — crash-resumed jobs are
        // scheduled immediately by JobRunner.InitializeAsync and their first progress events would be lost
        // by a subscriber attached any later. See IProgressHub's remarks.
        services.GetRequiredService<IProgressHub>();

        var settingManager = services.GetRequiredService<ISettingManager>();
        Log.Information("Loading settings...");
        await settingManager.Load().ConfigureAwait(true);

        services.GetRequiredService<IThemeService>().ApplyFromSettings();
        services.GetRequiredService<ILocalizationService>().SetLanguage(settingManager.Current.Appearance.Language);

        var service = services.GetRequiredService<IService>();
        Log.Information("Starting job runner...");
        await service.InitializeAsync().ConfigureAwait(true);

        // Fire-and-forget — an update check must never hold up startup or the splash screen.
        _ = UpdateChecker.CheckForUpdatesAsync();

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