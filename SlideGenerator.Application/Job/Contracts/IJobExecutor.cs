namespace SlideGenerator.Application.Job.Contracts;

/// <summary>
///     Executes a single sheet job.
///     Implementations should update sheet/group state and report progress via notifications.
/// </summary>
public interface IJobExecutor
{
    /// <summary>
    ///     Executes a sheet job.
    /// </summary>
    /// <param name="sheetId">The sheet job identifier.</param>
    /// <param name="cancellationToken">A token used to cancel execution.</param>
    Task ExecuteJobAsync(string sheetId, CancellationToken cancellationToken);
}