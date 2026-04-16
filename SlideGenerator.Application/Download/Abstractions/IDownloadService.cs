using System.ComponentModel;

namespace SlideGenerator.Application.Download.Abstractions;

public interface IDownloadService : IDisposable, IAsyncDisposable
{
    event EventHandler<AsyncCompletedEventArgs> DownloadFileCompleted;
    event EventHandler<IDownloadStartedEventArgs> DownloadStarted;
    Task DownloadFileTaskAsync(string url, string filePath);

    void Pause();
    void Resume();
    Task CancelTaskAsync();
}