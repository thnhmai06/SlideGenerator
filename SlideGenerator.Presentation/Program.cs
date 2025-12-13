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
using SlideGenerator.Infrastructure.Services.Download;
using SlideGenerator.Infrastructure.Services.Image;
using SlideGenerator.Infrastructure.Services.Job;
using SlideGenerator.Infrastructure.Services.Sheet;
using SlideGenerator.Infrastructure.Services.Slide;
using SlideGenerator.Presentation.Hubs;
using InfraSlideGenerator = SlideGenerator.Infrastructure.Services.Slide.SlideGenerator;

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

builder.Services.AddSingleton<IImageService, ImageService>();
builder.Services.AddSingleton<ISheetService, SheetService>();
builder.Services.AddSingleton<IDownloadService, DownloadService>();
builder.Services.AddSingleton<ISlideTemplateService, SlideTemplateService>();
builder.Services.AddSingleton<ISlideGeneratingService, SlideGeneratingService>();

builder.Services.AddSingleton<JobManager>();
builder.Services.AddSingleton<IJobManager>(sp => sp.GetRequiredService<JobManager>());
builder.Services.AddSingleton<ISlideGenerator, InfraSlideGenerator>();
builder.Services.AddSingleton<IJobNotifier, JobNotifier<SlideHub>>();
builder.Services.AddScoped<IJobExecutor, JobExecutor>();

var hangfireDbPath = ConfigHolder.Value.Job.HangfireDbPath;
var hangfireDbDir = Path.GetDirectoryName(hangfireDbPath);
if (!string.IsNullOrEmpty(hangfireDbDir)) Directory.CreateDirectory(hangfireDbDir);

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSQLiteStorage(hangfireDbPath));

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
app.MapHub<SlideHub>("/hubs/presentation");
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