using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Download.Services;

namespace SlideGenerator.Download;

public static class Registration
{
    public static IServiceCollection AddDownloadServices(this IServiceCollection services)
    {
        services.AddSingleton<DownloadService>();
        return services;
    }
}