namespace SlideGenerator.Application.Job.Contracts;

public interface IJobExecutor
{
    Task ExecuteJobAsync(string sheetId, CancellationToken cancellationToken);
}