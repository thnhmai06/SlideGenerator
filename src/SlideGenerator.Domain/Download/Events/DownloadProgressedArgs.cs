namespace SlideGenerator.Domain.Download.Events;

public class DownloadProgressedArgs(long bytesReceived, long totalBytes, double progressPercentage) : EventArgs
{
    public long BytesReceived { get; } = bytesReceived;
    public long TotalBytes { get; } = totalBytes;
    public double ProgressPercentage { get; } = progressPercentage;
}