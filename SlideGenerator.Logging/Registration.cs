using Microsoft.Extensions.DependencyInjection;
using Serilog.Sinks.PeriodicBatching;
using SlideGenerator.Logging.Sinks;

namespace SlideGenerator.Logging;

/// <summary>
///     Provides extension methods to register logging-related services into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Adds the custom database logging sink and associated infrastructure to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddWorkflowLogging(this IServiceCollection services)
    {
        // Register the sink implementation so it can receive IServiceScopeFactory via DI
        services.AddSingleton<WorkflowDatabaseSink>();

        return services;
    }
}
