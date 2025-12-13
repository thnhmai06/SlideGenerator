namespace SlideGenerator.Infrastructure.Exceptions.Job;

public class JobNotFoundException(string jobId)
    : InvalidOperationException($"Job '{jobId}' not found.")
{
    public string JobId { get; } = jobId;
}