namespace SlideGenerator.Infrastructure.Job.Exceptions;

public class JobNotFound(string jobId)
    : InvalidOperationException($"Job '{jobId}' not found.")
{
    public string JobId { get; } = jobId;
}