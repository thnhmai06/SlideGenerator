namespace SlideGenerator.Domain.Download.Enums;

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