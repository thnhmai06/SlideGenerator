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
using SlideGenerator.Infrastructure.Sheet.Services;
using SlideGenerator.Infrastructure.Slide.Services;
using SlideGenerator.Presentation.Hubs;

{
    var loaded = ConfigLoader.Load(ConfigHolder.Locker);
    if (loaded != null)
        ConfigHolder.Value = loaded;
    else ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker);
}

#region Builder

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(options => options.EnableDetailedErrors = ConfigHolder.Value.Server.Debug);
builder.Services.AddHttpClient();
builder.Services.AddLogging();

// Application Services
builder.Services.AddSingleton<IImageService, ImageService>();
builder.Services.AddSingleton<ISheetService, SheetService>();
builder.Services.AddSingleton<IDownloadService, DownloadService>();
builder.Services.AddSingleton<IDownloadClient>(sp => (IDownloadClient)sp.GetRequiredService<IDownloadService>());
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<ISlideTemplateManager, SlideTemplateManager>();
builder.Services.AddSingleton<SlideWorkingManager>();
builder.Services.AddSingleton<ISlideServices, SlideServices>();

// Job Services
builder.Services.AddSingleton<JobManager>();
builder.Services.AddSingleton<IJobManager>(sp => sp.GetRequiredService<JobManager>());
builder.Services.AddSingleton<IJobNotifier, JobNotifier<SlideHub>>();
builder.Services.AddScoped<IJobExecutor, JobExecutor>();
builder.Services.AddSingleton<IJobStateStore, HangfireJobStateStore>();
builder.Services.AddHostedService<JobRestoreHostedService>();

// Hangfire Setup
var dbPath = Path.Combine(AppContext.BaseDirectory, "Jobs.db");
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

#endregion

#region App

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

var host = ConfigHolder.Value.Server.Host;
if (!string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
    && !string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
    host = "127.0.0.1";

app.Urls.Clear();
app.Urls.Add($"http://{host}:{ConfigHolder.Value.Server.Port}");
await app.RunAsync();

#endregion

ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker);
