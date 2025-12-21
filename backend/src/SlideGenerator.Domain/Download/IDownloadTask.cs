using SlideGenerator.Domain.Download.Enums;
using SlideGenerator.Domain.Download.Events;

namespace SlideGenerator.Domain.Download;

public interface IDownloadTask
{
    string Url { get; }
    DirectoryInfo SaveFolder { get; init; }
    string FileName { get; }
    string FilePath { get; }
    DownloadStatus Status { get; }
    long TotalSize { get; }
    long DownloadedSize { get; }
    double Progress { get; }
    bool IsBusy { get; }
    bool IsPaused { get; }
    bool IsCancelled { get; }

    event EventHandler<DownloadStartedArgs>? DownloadStartedEvents;
    event EventHandler<DownloadProgressedArgs>? DownloadProgressedEvents;
    event EventHandler<DownloadCompletedArgs>? DownloadCompletedEvents;

    /// <summary>
    ///     Starts the download asynchronously.
    /// </summary>
    Task DownloadFileAsync();

    /// <summary>
    ///     Pauses the download.
    /// </summary>
    void Pause();

    /// <summary>
    ///     Resumes a paused download.
    /// </summary>
    void Resume();

    /// <summary>
    ///     Cancels the download.
    /// </summary>
    void Cancel();
}