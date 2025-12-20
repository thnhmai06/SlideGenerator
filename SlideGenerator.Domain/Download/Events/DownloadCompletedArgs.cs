namespace SlideGenerator.Domain.Download.Events;

public class DownloadCompletedArgs(bool success, string fileName, string filePath, Exception? error) : EventArgs
{
    public bool Success { get; } = success;
    public string FileName { get; } = fileName;
    public string FilePath { get; } = filePath;
    public Exception? Error { get; } = error;
}