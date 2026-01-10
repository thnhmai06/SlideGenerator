namespace SlideGenerator.Domain.Features.Downloads.Events;

public class DownloadStartedArgs(string url, string fileName, string filePath, long totalBytes) : EventArgs
{
    public string Url { get; } = url;
    public long Size { get; } = totalBytes;
    public string FileName { get; } = fileName;
    public string FilePath { get; } = filePath;
}