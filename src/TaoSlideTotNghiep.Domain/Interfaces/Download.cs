using Downloader;
using System.ComponentModel;

namespace TaoSlideTotNghiep.Domain.Interfaces;

public interface IImageDownloadTask
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
    event EventHandler<ErrorEventArgs> ErrorOccurred;
    event EventHandler<DownloadStartedEventArgs> DownloadStarted;
    event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;
    event EventHandler<AsyncCompletedEventArgs> DownloadFileCompleted;

    /// <summary>
    /// Starts the download process.
    /// </summary>
    /// <param name="httpClient">Optional HttpClient for URL correction.</param>
    Task StartAsync(HttpClient? httpClient = null);

    void Pause();
    void Resume();
    void Stop();
}