namespace SlideGenerator.Domain.Features.Downloads.Enums;

public enum DownloadStatus
{
    None,
    Created,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}