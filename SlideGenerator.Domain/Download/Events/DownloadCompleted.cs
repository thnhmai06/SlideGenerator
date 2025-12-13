namespace SlideGenerator.Domain.Download.Events;

public record DownloadCompleted(bool Success, string? FilePath, Exception? Error);