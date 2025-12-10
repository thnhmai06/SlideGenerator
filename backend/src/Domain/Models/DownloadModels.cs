using System.ComponentModel;
using Domain.Config;
using Domain.Exceptions;
using Downloader;
using HttpUtils = Domain.Utilities.HttpUtils;

namespace Domain.Models;

/// <summary>
/// Represents a generic download task wrapping Downloader.DownloadService.
/// </summary>
public abstract class DownloadTask : Model,
    IDisposable
{
    private readonly DownloadService _downloader;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    protected DownloadTask(string url, string savePath)
    {
        Url = url;
        SavePath = savePath;

        var config = BackendConfig.Instance.Download;

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
        DownloadStarted += CheckDownloadFunction;
    }

    // Properties
    public string Url { get; }
    public string SavePath { get; }
    public DownloadStatus Status => _downloader.Status;
    public long TotalSize => _downloader.Package?.TotalFileSize ?? 0;
    public long DownloadedSize => _downloader.Package?.ReceivedBytesSize ?? 0;
    public double Progress => TotalSize > 0 ? (double)DownloadedSize / TotalSize * 100 : 0;
    public bool IsBusy => _downloader.IsBusy;
    public bool IsPaused => _downloader.IsPaused;
    public bool IsCancelled => _downloader.IsCancelled;


    // Events
    public event EventHandler<ErrorEventArgs> ErrorOccurred = delegate { };

    public event EventHandler<DownloadStartedEventArgs> DownloadStarted
    {
        add => _downloader.DownloadStarted += value;
        remove => _downloader.DownloadStarted -= value;
    }

    public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged
    {
        add => _downloader.DownloadProgressChanged += value;
        remove => _downloader.DownloadProgressChanged -= value;
    }

    public event EventHandler<AsyncCompletedEventArgs> DownloadFileCompleted
    {
        add => _downloader.DownloadFileCompleted += value;
        remove => _downloader.DownloadFileCompleted -= value;
    }

    /// <summary>
    /// Starts the download process.
    /// </summary>
    /// <param name="httpClient">Optional HttpClient for URL correction.</param>
    public async Task StartAsync(HttpClient? httpClient = null)
    {
        if (Status == DownloadStatus.Stopped) return;

        try
        {
            var correctedUrl = httpClient != null
                ? await HttpUtils.CorrectImageUrlAsync(Url, httpClient)
                : Url;

            var directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            await _downloader.DownloadFileTaskAsync(correctedUrl, SavePath, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Download was cancelled
        }
        catch (Exception ex)
        {
            ErrorOccurred.Invoke(this, new ErrorEventArgs(ex));
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

    public void Stop()
    {
        _cts.Cancel();
        _downloader.CancelAsync();
    }

    /// <summary>
    /// Checks the download parameters when the download starts.
    /// </summary>
    /// <param name="e">Download started event arguments.</param>
    /// <returns>An exception if the download is invalid; otherwise, null.</returns>
    protected abstract Exception? CheckDownload(DownloadStartedEventArgs e);

    private void CheckDownloadFunction(object? sender, DownloadStartedEventArgs e)
    {
        var checkException = CheckDownload(e);
        if (checkException is not null)
        {
            Stop();
            ErrorOccurred.Invoke(this, new ErrorEventArgs(checkException));
            throw checkException;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _downloader.DownloadStarted -= CheckDownloadFunction;
        _downloader.Dispose();
        _cts.Dispose();

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Represents an Image download task.
/// </summary>
public class ImageDownloadTask(string url, string savePath) : DownloadTask(url, savePath)
{
    protected override Exception? CheckDownload(DownloadStartedEventArgs e)
    {
        var extension = Path.GetExtension(e.FileName).TrimStart('.');
        if (string.IsNullOrEmpty(extension) || !BackendConfig.ImageExtensions.Contains(extension))
            return new FileExtensionNotSupportedException(extension);
        return null;
    }
}