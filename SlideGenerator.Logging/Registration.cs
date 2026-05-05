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
    /// <param name="services">The service collection to add logging services to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Adds system-centric Serilog logging backed by an asynchronous daily rolling file sink.
        /// </summary>
        /// <param name="configuration">The application configuration used to resolve Serilog settings.</param>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddSystemLogging(IConfiguration configuration)
        {
            Directory.CreateDirectory(LoggingPaths.LogFolderPath);

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .WriteTo.Async(a => a.File(
                    Path.Combine(LoggingPaths.LogFolderPath, ".log"),
                    rollingInterval: RollingInterval.Day, // create timestamp
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"))
                .CreateLogger();

            Log.Logger = logger;
            services.AddSerilog(logger, dispose: true);

            return services;
        }

        /// <summary>
        ///     Adds the custom database logging sink and associated infrastructure to the service collection.
        /// </summary>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddWorkflowLogging()
        {
            // Register the sink implementation so it can receive IServiceScopeFactory via DI
            services.AddSingleton<WorkflowDatabaseSink>();

            return services;
        }
    }
}
