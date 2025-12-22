using System.Net;
using System.Net.Sockets;
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
builder.Services.AddSingleton<IDownloadClient>(sp => (IDownloadClient)sp.GetRequiredService<IDownloadService>());
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

#endregion

#region App

var app = builder.Build();
app.UseCors();
app.UseWebSockets();

app.Lifetime.ApplicationStopping.Register(() => { ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker); });

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

var requestedPort = ConfigHolder.Value.Server.Port;
var resolvedPort = FindAvailablePort(host, requestedPort, 20);
if (resolvedPort != requestedPort)
{
    var current = ConfigHolder.Value;
    ConfigHolder.Value = new Config
    {
        Server = new Config.ServerConfig
        {
            Host = host,
            Port = resolvedPort,
            Debug = current.Server.Debug
        },
        Download = current.Download,
        Job = current.Job,
        Image = current.Image
    };
    ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker);
    app.Logger.LogWarning(
        "Port {RequestedPort} is in use. Using {ResolvedPort} instead.",
        requestedPort,
        resolvedPort);
}

app.Urls.Clear();
app.Urls.Add($"http://{host}:{ConfigHolder.Value.Server.Port}");
await app.RunAsync();

#endregion

ConfigLoader.Save(ConfigHolder.Value, ConfigHolder.Locker);

static int FindAvailablePort(string host, int startPort, int maxAttempts)
{
    for (var i = 0; i < maxAttempts; i++)
    {
        var port = startPort + i;
        if (IsPortAvailable(host, port))
            return port;
    }

    return startPort;
}

static bool IsPortAvailable(string host, int port)
{
    IPAddress address;
    if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        address = IPAddress.Loopback;
    else if (!IPAddress.TryParse(host, out address!))
        address = IPAddress.Loopback;

    try
    {
        var listener = new TcpListener(address, port);
        listener.Start();
        listener.Stop();
        return true;
    }
    catch (SocketException)
    {
        return false;
    }
}
