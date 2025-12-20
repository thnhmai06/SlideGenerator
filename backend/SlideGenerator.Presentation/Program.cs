using Hangfire;
using Hangfire.Storage.SQLite;
using SlideGenerator.Application.Configs;
using SlideGenerator.Application.Configs.Models;
using SlideGenerator.Application.Download.Contracts;
using SlideGenerator.Application.Image.Contracts;
using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Application.Sheet.Contracts;
using SlideGenerator.Application.Slide.Contracts;
using SlideGenerator.Infrastructure.Configs;
using SlideGenerator.Infrastructure.Download.Services;
using SlideGenerator.Infrastructure.Image.Services;
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
builder.Services.AddSingleton<ISlideTemplateManager, SlideTemplateManager>();
builder.Services.AddSingleton<ISlideWorkingManager, SlideWorkingManager>();
builder.Services.AddSingleton<ISlideServices, SlideServices>();

// Job Services
builder.Services.AddSingleton<IJobManager, JobManager>();
builder.Services.AddSingleton<IJobNotifier, JobNotifier<SlideHub>>();
builder.Services.AddScoped<IJobExecutor, JobExecutor>();

// Hangfire Setup
var dbPath = ConfigHolder.Value.Job.DatabasePath;
var dbDir = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDir)) Directory.CreateDirectory(dbDir);

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

app.UseHangfireDashboard("/dashboard", new DashboardOptions
{
    Authorization = []
});

app.Urls.Add($"http://{ConfigHolder.Value.Server.Host}:{ConfigHolder.Value.Server.Port}");
await app.RunAsync();

#endregion

ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker);