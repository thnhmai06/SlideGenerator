using Application.Contracts;
using Domain.Config;
using Infrastructure.Services;
using Presentation.Hubs;

var config = BackendConfig.Instance;

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
app.MapHub<ImageHub>("/hubs/image");
app.MapHub<SheetHub>("/hubs/sheet");
app.MapHub<DownloadHub>("/hubs/download");
app.MapGet("/", () => new
{
    Name = BackendConfig.AppName,
    Description = BackendConfig.AppDescription,
    IsRunning = true
});
app.MapGet("/health", () => Results.Ok(new { IsRunning = true }));
app.Urls.Add($"http://{config.Server.Host}:{config.Server.Port}");
app.Run();

#endregion