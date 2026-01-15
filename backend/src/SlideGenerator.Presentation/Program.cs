using System.Text.Json;
using Hangfire;
using Hangfire.Storage.SQLite;
using SlideGenerator.Application.Features.Configs;
using SlideGenerator.Application.Features.Downloads;
using SlideGenerator.Application.Features.Images;
using SlideGenerator.Application.Features.Jobs.Contracts;
using SlideGenerator.Application.Features.Sheets;
using SlideGenerator.Application.Features.Slides;
using SlideGenerator.Domain.Configs;
using SlideGenerator.Domain.Features.Downloads;
using SlideGenerator.Domain.Features.IO;
using SlideGenerator.Domain.Features.Jobs.Interfaces;
using SlideGenerator.Infrastructure.Common.Logging;
using SlideGenerator.Infrastructure.Features.Configs;
using SlideGenerator.Infrastructure.Features.Downloads.Services;
using SlideGenerator.Infrastructure.Features.Images.Services;
using SlideGenerator.Infrastructure.Features.IO;
using SlideGenerator.Infrastructure.Features.Jobs.Services;
using SlideGenerator.Infrastructure.Features.Sheets.Services;
using SlideGenerator.Infrastructure.Features.Slides.Services;
using SlideGenerator.Presentation.Features.Configs;
using SlideGenerator.Presentation.Features.Jobs;
using SlideGenerator.Presentation.Features.Sheets;

namespace SlideGenerator.Presentation;

/// <summary>
///     The main class of SlideGenerator Presentation layer.
/// </summary>
public static class Program
{
    private static void LoadConfig()
    {
        var loaded = ConfigLoader.Load(ConfigHolder.Locker);
        if (loaded != null)
            ConfigHolder.Value = loaded;
        else ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker);
    }

    private static WebApplicationBuilder InitializeBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog from Infrastructure
        builder.AddInfrastructureLogging();

        builder.Services.AddSignalR(options => options.EnableDetailedErrors = ConfigHolder.Value.Server.Debug)
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.PayloadSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            });
        builder.Services.AddHttpClient();
        builder.Services.AddLogging();

        // Application Services
        builder.Services.AddSingleton<IImageService, ImageService>();
        builder.Services.AddSingleton<ISheetService, SheetService>();
        builder.Services.AddSingleton<IDownloadService, DownloadService>();
        builder.Services.AddSingleton(sp => (IDownloadClient)sp.GetRequiredService<IDownloadService>());
        builder.Services.AddSingleton<IFileSystem, FileSystem>();
        builder.Services.AddSingleton<ISlideTemplateManager, SlideTemplateManager>();
        builder.Services.AddSingleton<SlideWorkingManager>();
        builder.Services.AddSingleton<ISlideWorkingManager>(sp => sp.GetRequiredService<SlideWorkingManager>());
        builder.Services.AddSingleton<ISlideServices, SlideServices>();

        // Job Services
        builder.Services.AddSingleton<JobManager>();
        builder.Services.AddSingleton<IJobManager>(sp => sp.GetRequiredService<JobManager>());
        builder.Services.AddSingleton<IJobNotifier, JobNotifier<JobHub>>();
        builder.Services.AddScoped<IJobExecutor, JobExecutor>();
        builder.Services.AddSingleton<IJobStateStore, HangfireJobStateStore>();
        builder.Services.AddHostedService<JobRestoreHostedService>();

        // Hangfire Setup
        var dbPath = Config.DefaultDatabasePath;
        builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSQLiteStorage(dbPath));
        builder.Services.AddHangfireServer(options =>
        {
            options.WorkerCount = ConfigHolder.Value.Job.MaxConcurrentJobs;
        });

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });
        });

        return builder;
    }

    private static WebApplication InitializeApp(WebApplicationBuilder builder)
    {
        var app = builder.Build();
        app.UseCors();
        app.UseWebSockets();

        app.MapHub<SheetHub>("/hubs/sheet");
        app.MapHub<JobHub>("/hubs/job");
        app.MapHub<JobHub>("/hubs/task");
        app.MapHub<ConfigHub>("/hubs/config");

        app.MapGet("/", () => new
        {
            Name = Config.AppName,
            Description = Config.AppDescription,
            Repository = Config.AppUrl
        });
        app.MapGet("/health", () => Results.Ok(new { IsRunning = true }));
        app.UseHangfireDashboard("/dashboard", new DashboardOptions
        {
            DashboardTitle = Config.AppName,
            Authorization = [],
            IsReadOnlyFunc = _ => true
        });

        // Get host/port
        var host = ConfigHolder.Value.Server.Host;
        app.Urls.Clear();
        app.Urls.Add($"http://{host}:{ConfigHolder.Value.Server.Port}");

        // On Application Stopping
        app.Lifetime.ApplicationStopping.Register(() =>
        {
            ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker);
        });

        return app;
    }

    private static async Task Main(string[] args)
    {
        LoadConfig();

        try
        {
            var builder = InitializeBuilder(args);
            var app = InitializeApp(builder);
            await app.RunAsync();
        }
        finally
        {
            ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker);
            await LoggingExtensions.CloseAndFlushAsync();
        }
    }
}