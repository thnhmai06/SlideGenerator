namespace TaoSlideTotNghiep.Infrastructure.Services.Job;

public interface IJobExecutor
{
    Task ExecuteJobAsync(string jobId, CancellationToken cancellationToken);
}