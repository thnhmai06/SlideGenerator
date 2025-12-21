namespace SlideGenerator.Application.Job.Contracts;

/// <summary>
///     Executes sheet jobs in the background worker.
/// </summary>
public interface IJobExecutor
{
    /// <summary>
    ///     Executes a sheet job by id in a background worker.
    /// </summary>
    Task ExecuteJobAsync(string sheetId, CancellationToken cancellationToken);
}