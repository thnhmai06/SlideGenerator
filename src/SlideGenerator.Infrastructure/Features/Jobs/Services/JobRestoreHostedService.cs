using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SlideGenerator.Infrastructure.Features.Jobs.Services;

/// <summary>
///     Restores unfinished jobs from persisted state on startup.
/// </summary>
public sealed class JobRestoreHostedService(JobManager jobManager, ILogger<JobRestoreHostedService> logger)
    : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await jobManager.RestoreAsync(cancellationToken);
        logger.LogInformation("Job state restoration completed");
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}