using TaoSlideTotNghiep.Application.Contracts;
using TaoSlideTotNghiep.Infrastructure.Services;
using TaoSlideTotNghiep.Infrastructure.Config;
using TaoSlideTotNghiep.Presentation.Hubs;

ConfigManager.Load();
var config = ConfigManager.Value;

#region Builder

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(options => options.EnableDetailedErrors = config.Server.Debug);
builder.Services.AddHttpClient();
builder.Services.AddLogging();
builder.Services.AddSingleton<IImageService, ImageService>();
builder.Services.AddSingleton<ISheetService, SheetService>();
builder.Services.AddSingleton<IDownloadService, DownloadService>();
builder.Services.AddSingleton<ITemplateService, TemplateService>();
builder.Services.AddSingleton<IGeneratingService, GeneratingService>();
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
app.MapGet("/", () => new
{
    Name = ConfigModel.AppName,
    Description = ConfigModel.AppDescription,
    IsRunning = true
});
app.MapGet("/health", () => Results.Ok(new { IsRunning = true }));
app.Urls.Add($"http://{config.Server.Host}:{config.Server.Port}");
await app.RunAsync();

#endregion

ConfigManager.Save();