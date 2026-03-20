using SlideGenerator.Domain.Download.Models;

namespace SlideGenerator.Domain.Download.Entities;

public interface IDownloader
{
    DownloadRequest Request { get; }
    string FilePath { get; }

    event EventHandler<object> DownloadFileCompleted;

    Task DownloadAsync();
    void Pause();
    void Resume();
    Task Cancel();
}