using SlideGenerator.Domain.Download.Enums;
using SlideGenerator.Domain.Download.Events;

namespace SlideGenerator.Domain.Download.Interfaces;

public interface IDownloadTask : IDisposable
{
    string Url { get; }
    string SavePath { get; }
    DownloadStatus Status { get; }
    long TotalSize { get; }
    long DownloadedSize { get; }
    double Progress { get; }
    bool IsBusy { get; }
    bool IsPaused { get; }
    bool IsCancelled { get; }

    event EventHandler<Exception>? ErrorOccurred;
    event EventHandler<DownloadProgressed>? ProgressChanged;
    event EventHandler<DownloadCompleted>? Completed;

    Task StartAsync(CancellationToken cancellationToken = default);
    void Pause();
    void Resume();
    void Cancel();
}