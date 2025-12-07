using TaoSlideTotNghiep.Config;
using TaoSlideTotNghiep.Hubs;
using TaoSlideTotNghiep.Services;

var builder = WebApplication.CreateBuilder(args);

// Load application configuration
var config = AppConfig.Instance;

// Add services
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = config.Server.Debug;
});

builder.Services.AddHttpClient();
builder.Services.AddLogging();

// Register application services
builder.Services.AddSingleton<IImageService, ImageService>();
builder.Services.AddSingleton<ISheetService, SheetService>();
builder.Services.AddSingleton<IDownloadService, DownloadService>();

// Add CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware
app.UseCors();

// Map SignalR hubs
app.MapHub<ImageHub>("/hubs/image");
app.MapHub<SheetHub>("/hubs/sheet");
app.MapHub<DownloadHub>("/hubs/download");

// Health check endpoint
app.MapGet("/", () => new
{
    Name = AppConfig.AppName,
    Type = AppConfig.AppType,
    Description = AppConfig.AppDescription,
    Status = "Running"
});

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.Run();
