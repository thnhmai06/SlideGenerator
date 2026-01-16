namespace SlideGenerator.Application.Features.Jobs.Contracts;

/// <summary>
///     Executes sheet jobs in the background worker.
/// </summary>
public interface IJobExecutor
{
    /// <summary>
    ///     Executes a sheet job by id in a background worker.
    ///     The job will be displayed in Hangfire dashboard as "WorkbookName/SheetName".
    /// </summary>
    Task ExecuteJobAsync(string sheetId, CancellationToken cancellationToken);
}