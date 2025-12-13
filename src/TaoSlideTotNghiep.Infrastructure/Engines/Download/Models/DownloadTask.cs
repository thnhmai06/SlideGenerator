using Downloader;
using System.ComponentModel;
using TaoSlideTotNghiep.Application.Configs;
using TaoSlideTotNghiep.Domain.Download.Events;
using TaoSlideTotNghiep.Domain.Download.Interfaces;
using DownloadStatus = TaoSlideTotNghiep.Domain.Download.Enums.DownloadStatus;

namespace TaoSlideTotNghiep.Infrastructure.Engines.Download.Models;

/// <summary>
/// Represents a generic download task wrapping Downloader.DownloadService.
/// </summary>
public abstract class DownloadTask : Model, IDownloadTask
{
    private readonly DownloadService _downloader;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    protected DownloadTask(string url, string savePath)
    {
        Url = url;
        SavePath = savePath;

        var config = ConfigHolder.Value.Download;

        var downloadOptions = new DownloadConfiguration
        {
            ChunkCount = config.MaxChunks,
            ParallelDownload = true,
            MaximumBytesPerSecond = config.LimitBytesPerSecond,
            Timeout = config.Retry.Timeout * 1000,
            MaxTryAgainOnFailure = config.Retry.MaxRetries,
            ClearPackageOnCompletionWithFailure = true
        };

        _downloader = new DownloadService(downloadOptions);
        _downloader.DownloadStarted += OnDownloadStarted;
        _downloader.DownloadProgressChanged += OnProgressChanged;
        _downloader.DownloadFileCompleted += OnCompleted;
    }

    // Properties
    public string Url { get; }
    public string SavePath { get; }

    public DownloadStatus Status => _downloader.Status switch
    {
        Downloader.DownloadStatus.None => DownloadStatus.None,
        Downloader.DownloadStatus.Created => DownloadStatus.Created,
        Downloader.DownloadStatus.Running => DownloadStatus.Running,
        Downloader.DownloadStatus.Paused => DownloadStatus.Paused,
        Downloader.DownloadStatus.Completed => DownloadStatus.Completed,
        Downloader.DownloadStatus.Failed => DownloadStatus.Failed,
        Downloader.DownloadStatus.Stopped => DownloadStatus.Cancelled,
        _ => DownloadStatus.None
    };

    public long TotalSize => _downloader.Package?.TotalFileSize ?? 0;
    public long DownloadedSize => _downloader.Package?.ReceivedBytesSize ?? 0;
    public double Progress => TotalSize > 0 ? (double)DownloadedSize / TotalSize * 100 : 0;
    public bool IsBusy => _downloader.IsBusy;
    public bool IsPaused => _downloader.IsPaused;
    public bool IsCancelled => _downloader.IsCancelled;


    // Events
    public event EventHandler<Exception>? ErrorOccurred;
    public event EventHandler<DownloadProgressed>? ProgressChanged;
    public event EventHandler<DownloadCompleted>? Completed;

    /// <summary>
    /// Starts the download process.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token for the download.</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (Status == DownloadStatus.Cancelled) return;

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);

            var directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            await _downloader.DownloadFileTaskAsync(Url, SavePath, linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Download was cancelled
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    public void Pause()
    {
        _downloader.Pause();
    }

    public void Resume()
    {
        _downloader.Resume();
    }

    public void Cancel()
    {
        _cts.Cancel();
        _downloader.CancelAsync();
    }

    /// <summary>
    /// Checks the download parameters when the download starts.
    /// </summary>
    /// <param name="e">Download started event arguments.</param>
    /// <returns>An exception if the download is invalid; otherwise, null.</returns>
    protected abstract Exception? ValidateDownload(DownloadStartedEventArgs e);

    private void OnDownloadStarted(object? sender, DownloadStartedEventArgs e)
    {
        var validationException = ValidateDownload(e);
        if (validationException is not null)
        {
            Cancel();
            ErrorOccurred?.Invoke(this, validationException);
        }
    }

    private void OnProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        ProgressChanged?.Invoke(this, new DownloadProgressed(
            e.ReceivedBytesSize,
            e.TotalBytesToReceive,
            e.ProgressPercentage));
    }

    private void OnCompleted(object? sender, AsyncCompletedEventArgs e)
    {
        var success = e.Error == null && !e.Cancelled;
        Completed?.Invoke(this, new DownloadCompleted(
            success,
            success ? SavePath : null,
            e.Error));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _downloader.DownloadStarted -= OnDownloadStarted;
        _downloader.DownloadProgressChanged -= OnProgressChanged;
        _downloader.DownloadFileCompleted -= OnCompleted;
        _downloader.Dispose();
        _cts.Dispose();

        GC.SuppressFinalize(this);
    }
}