using TaoSlideTotNghiep.Config;
using TaoSlideTotNghiep.Hubs;
using TaoSlideTotNghiep.Services;

var config = AppConfig.Instance;

//? Builder
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(options => options.EnableDetailedErrors = config.Server.Debug);
builder.Services.AddHttpClient();
builder.Services.AddLogging();
builder.Services.AddSingleton<IImageService, ImageService>();
builder.Services.AddSingleton<ISheetService, SheetService>();
builder.Services.AddSingleton<IDownloadService, DownloadService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

//? App
var app = builder.Build();
app.UseCors();
app.UseWebSockets();
app.MapHub<ImageHub>("/hubs/image");
app.MapHub<SheetHub>("/hubs/sheet");
app.MapHub<DownloadHub>("/hubs/download");
app.MapGet("/", () => new
{
    Name = AppConfig.AppName,
    Type = AppConfig.AppType,
    Description = AppConfig.AppDescription,
    IsRunning = true
});
app.MapGet("/health", () => Results.Ok(new { IsRunning = true }));
app.Run();
