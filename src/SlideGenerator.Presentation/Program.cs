using System.Text.Json;
using Hangfire;
using Hangfire.Storage.SQLite;
using SlideGenerator.Application.Configs;
using SlideGenerator.Application.Download;
using SlideGenerator.Application.Image;
using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Application.Sheet;
using SlideGenerator.Application.Slide;
using SlideGenerator.Domain.Configs;
using SlideGenerator.Domain.Download;
using SlideGenerator.Domain.IO;
using SlideGenerator.Domain.Job.Interfaces;
using SlideGenerator.Infrastructure.Configs;
using SlideGenerator.Infrastructure.Download.Services;
using SlideGenerator.Infrastructure.Image.Services;
using SlideGenerator.Infrastructure.IO;
using SlideGenerator.Infrastructure.Job.Services;
using SlideGenerator.Infrastructure.Logging;
using SlideGenerator.Infrastructure.Sheet.Services;
using SlideGenerator.Infrastructure.Slide.Services;
using SlideGenerator.Presentation.Hubs;

/// <summary>
/// The main class of SlideGenerator Presentation layer.
/// </summary>
public partial class Program
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
        builder.Services.AddSingleton<IJobNotifier, JobNotifier<SlideHub>>();
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
        builder.Services.AddHangfireServer(options => { options.WorkerCount = ConfigHolder.Value.Job.MaxConcurrentJobs; });

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
        app.MapHub<SlideHub>("/hubs/slide");
        app.MapHub<ConfigHub>("/hubs/config");

        app.MapGet("/", () => new
        {
            Name = Config.AppName,
            Description = Config.AppDescription,
            IsRunning = true
        });
        app.MapGet("/health", () => Results.Ok(new { IsRunning = true }));
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [],
            IsReadOnlyFunc = _ => true
        });

        // Get host/port
        var host = ConfigHolder.Value.Server.Host;
        app.Urls.Clear();
        app.Urls.Add($"http://{host}:{ConfigHolder.Value.Server.Port}");

        // On Application Stopping
        app.Lifetime.ApplicationStopping.Register(
            () => { ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker); });

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