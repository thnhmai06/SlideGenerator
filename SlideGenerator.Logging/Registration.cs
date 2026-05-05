using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using SlideGenerator.Logging.Sinks;

namespace SlideGenerator.Logging;

/// <summary>
///     Provides extension methods to register logging-related services into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Configures the static Serilog logger using the provided configuration.
    /// </summary>
    /// <param name="configuration">The application configuration used to resolve Serilog settings.</param>
    public static void ConfigureStaticLogger(IConfiguration configuration)
    {
        if (!Directory.Exists(LoggingPaths.LogFolderPath))
        {
            Directory.CreateDirectory(LoggingPaths.LogFolderPath);
        }

        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Async(a => a.File(
                Path.Combine(LoggingPaths.LogFolderPath, ".log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"))
            .CreateLogger();

        Log.Logger = logger;
    }

    /// <summary>
    ///     Adds system-centric Serilog logging backed by an asynchronous daily rolling file sink.
    /// </summary>
    /// <param name="services">The service collection to add logging services to.</param>
    /// <param name="configuration">The application configuration used to resolve Serilog settings.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSystemLogging(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureStaticLogger(configuration);
        services.AddSerilog(Log.Logger, dispose: true);

        return services;
    }

    /// <summary>
    ///     Adds the custom database logging sink and associated infrastructure to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddWorkflowLogging(this IServiceCollection services)
    {
        // Register the sink implementation so it can receive IServiceScopeFactory via DI
        services.AddSingleton<WorkflowDatabaseSink>();

        return services;
    }
}
