using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlideGenerator.Coordinator.Services;
using SlideGenerator.Settings.Interfaces;

namespace SlideGenerator.Coordinator;

public static class Registration
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<GateLocker>();
        return services;
    }
}