using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Coordinator.Services;

namespace SlideGenerator.Coordinator;

public static class Registration
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<GateLocker>();
        return services;
    }
}