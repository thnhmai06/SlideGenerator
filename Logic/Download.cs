using System.ComponentModel;
using Downloader;
using TaoSlideTotNghiep.Config;
using TaoSlideTotNghiep.Exceptions;
using TaoSlideTotNghiep.Utils;

namespace TaoSlideTotNghiep.Logic;

/// <summary>
/// Enumeration of download statuses.
/// </summary>
public enum DownloadStatus
{
    Queued,
    Connecting,
    Downloading,
    Paused,
    Completed,
    Error,
    Stopped
}

/// <summary>
/// Represents a download task using Downloader library with pause/resume support.
/// </summary>
public class ImageDownloadTask : IDisposable
{
    private readonly DownloadService _downloader;
    private readonly CancellationTokenSource _cts = new();

    private bool _disposed;

    public event Action<double>? ProgressChanged;
    public event Action<DownloadStatus>? StatusChanged;
    public event Action<Exception>? ErrorOccurred;

    public ImageDownloadTask(string url, string savePath, HttpClient? httpClient = null)
    {
        Url = url;
        SavePath = savePath;

        var config = AppConfig.Instance.Download;
        
        var downloadOptions = new DownloadConfiguration
        {
            ChunkCount = config.MaxWorkers,
            ParallelDownload = config.MaxWorkers > 1,
            Timeout = config.Timeout.Request * 1000
        };

        _downloader = new DownloadService(downloadOptions);

        // Subscribe to events
        _downloader.DownloadStarted += OnDownloadStarted;
        _downloader.DownloadProgressChanged += OnDownloadProgressChanged;
        _downloader.DownloadFileCompleted += OnDownloadFileCompleted;
    }

    public string Url { get; }

    public string SavePath { get; }

    public DownloadStatus Status { get; private set; } = DownloadStatus.Queued;

    public long TotalSize => _downloader.Package?.TotalFileSize ?? 0;
    public long DownloadedSize => _downloader.Package?.ReceivedBytesSize ?? 0;
    public double Progress => TotalSize > 0 ? (double)DownloadedSize / TotalSize * 100 : 0;
    public bool IsCompleted => Status == DownloadStatus.Completed;
    public bool IsPaused => Status == DownloadStatus.Paused;

    /// <summary>
    /// Starts the download process.
    /// </summary>
    public async Task StartAsync(HttpClient? httpClient = null)
    {
        if (Status == DownloadStatus.Stopped) return;

        try
        {
            SetStatus(DownloadStatus.Connecting);
            
            // Correct URL for cloud storage services
            var correctedUrl = httpClient != null 
                ? await HttpUtils.CorrectImageUrl(Url, httpClient)
                : Url;

            var directory = Path.GetDirectoryName(SavePath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await _downloader.DownloadFileTaskAsync(correctedUrl, SavePath, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Download was cancelled
        }
        catch (Exception ex)
        {
            SetStatus(DownloadStatus.Error);
            ErrorOccurred?.Invoke(ex);
            throw;
        }
    }

    /// <summary>
    /// Pauses the download.
    /// </summary>
    public void Pause()
    {
        _downloader.Pause();
        SetStatus(DownloadStatus.Paused);
    }

    /// <summary>
    /// Resumes the download.
    /// </summary>
    public void Resume()
    {
        _downloader.Resume();
        SetStatus(DownloadStatus.Downloading);
    }

    /// <summary>
    /// Stops the download and cancels all operations.
    /// </summary>
    public void Stop()
    {
        _cts.Cancel();
        _downloader.CancelAsync();
        SetStatus(DownloadStatus.Stopped);
    }

    private void OnDownloadStarted(object? sender, DownloadStartedEventArgs e)
    {
        // Validate image extension
        var extension = Path.GetExtension(e.FileName)?.TrimStart('.');
        if (!string.IsNullOrEmpty(extension) && !AppConfig.ImageExtensions.Contains(extension))
        {
            Stop();
            ErrorOccurred?.Invoke(new FileExtensionNotSupportedException(extension));
            return;
        }

        SetStatus(DownloadStatus.Downloading);
    }

    private void OnDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        ProgressChanged?.Invoke(e.ProgressPercentage);
    }

    private void OnDownloadFileCompleted(object? sender, AsyncCompletedEventArgs e)
    {
        if (e.Cancelled)
        {
            SetStatus(DownloadStatus.Stopped);
        }
        else if (e.Error != null)
        {
            SetStatus(DownloadStatus.Error);
            ErrorOccurred?.Invoke(e.Error);
        }
        else
        {
            SetStatus(DownloadStatus.Completed);
        }
    }

    private void SetStatus(DownloadStatus status)
    {
        Status = status;
        StatusChanged?.Invoke(status);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _downloader.DownloadStarted -= OnDownloadStarted;
        _downloader.DownloadProgressChanged -= OnDownloadProgressChanged;
        _downloader.DownloadFileCompleted -= OnDownloadFileCompleted;
        
        _downloader.Dispose();
        _cts.Dispose();
        
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Validates image downloads.
/// </summary>
public static class ImageDownloadValidator
{
    public static void ValidateExtension(string? extension)
    {
        if (!string.IsNullOrEmpty(extension) && !AppConfig.ImageExtensions.Contains(extension))
        {
            throw new FileExtensionNotSupportedException(extension);
        }
    }
}
