using Microsoft.AspNetCore.Builder;
using Serilog;

namespace SlideGenerator.Infrastructure.Logging;

/// <summary>
///     Provides extension methods for setting up logging within the infrastructure layer.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    ///     Configures Serilog for the application, reading configuration from appsettings and environment variables.
    ///     It sets up console logging and file logging if the SLIDEGEN_LOG_PATH environment variable is provided.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder" /> to configure.</param>
    public static void AddInfrastructureLogging(this WebApplicationBuilder builder)
    {
        var logPath = Environment.GetEnvironmentVariable("SLIDEGEN_LOG_PATH");

        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console();

        if (!string.IsNullOrWhiteSpace(logPath)) loggerConfig.WriteTo.File(logPath);

        builder.Host.UseSerilog(loggerConfig.CreateLogger());
    }

    /// <summary>
    ///     Statically closes and flushes the global <see cref="Log.Logger" />, ensuring all buffered logs are written.
    ///     This should be called on application shutdown.
    /// </summary>
    /// <returns>A task that completes when the logger is flushed.</returns>
    public static async Task CloseAndFlushAsync()
    {
        await Log.CloseAndFlushAsync();
    }
}