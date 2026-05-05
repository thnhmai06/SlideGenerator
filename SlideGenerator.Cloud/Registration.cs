using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Cloud.Services;

namespace SlideGenerator.Cloud;

public static class Registration
{
    public static IServiceCollection AddCloudServices(this IServiceCollection services)
    {
        services.AddSingleton<MultiCloudResolver>();
        return services;
    }
}
