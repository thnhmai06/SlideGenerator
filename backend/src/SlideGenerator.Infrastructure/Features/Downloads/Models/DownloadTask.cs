using System.ComponentModel;
using Downloader;
using Microsoft.Extensions.Logging;
using SlideGenerator.Application.Features.Configs;
using SlideGenerator.Domain.Features.Downloads;
using SlideGenerator.Domain.Features.Downloads.Events;
using DownloadStatus = SlideGenerator.Domain.Features.Downloads.Enums.DownloadStatus;

namespace SlideGenerator.Infrastructure.Features.Downloads.Models;

public abstract class DownloadTask : IDownloadTask, IDisposable
{
    private readonly DownloadService _downloader;
    private bool _disposed;

    protected DownloadTask(string url, DirectoryInfo saveFolder,
        RequestConfiguration? requestConfiguration = null, ILoggerFactory? loggerFactory = null)
    {
        Url = url;
        SaveFolder = saveFolder;

        var config = ConfigHolder.Value.Download;
        _downloader = new DownloadService(new DownloadConfiguration
        {
            RequestConfiguration =
                requestConfiguration
                ?? new RequestConfiguration { Proxy = config.Proxy.GetWebProxy() },
            ChunkCount = config.MaxChunks,
            ParallelDownload = true,
            MaximumBytesPerSecond = config.LimitBytesPerSecond,
            Timeout = config.Retry.Timeout * 1000,
            MaxTryAgainOnFailure = config.Retry.MaxRetries,
            ClearPackageOnCompletionWithFailure = true
        }, loggerFactory);

        // Event hooks
        _downloader.DownloadStarted += OnDownloadStarted;
        _downloader.DownloadProgressChanged += OnDownloadProgressed;
        _downloader.DownloadFileCompleted += OnDownloadCompleted;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _downloader.Dispose();

        GC.SuppressFinalize(this);
    }

    public string Url { get; protected set; }
    public DirectoryInfo SaveFolder { get; init; }
    public string FileName => _downloader.Package.FileName;

    public string FilePath
    {
        get
        {
            if (string.IsNullOrEmpty(FileName))
                return string.Empty;
            if (string.IsNullOrEmpty(field))
                field = Path.Combine(SaveFolder.FullName, FileName);
            return field;
        }
    } = string.Empty;

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
    public event EventHandler<DownloadStartedArgs>? DownloadStartedEvents;
    public event EventHandler<DownloadProgressedArgs>? DownloadProgressedEvents;
    public event EventHandler<DownloadCompletedArgs>? DownloadCompletedEvents;

    public virtual async Task DownloadFileAsync()
    {
        if (Status == DownloadStatus.Cancelled) return;

        try
        {
            if (!SaveFolder.Exists) SaveFolder.Create();
            await _downloader.DownloadFileTaskAsync(Url, SaveFolder);
        }
        catch (IOException e)
        {
            DownloadCompletedEvents?.Invoke(this,
                new DownloadCompletedArgs(false, FileName, FilePath, e));
        }
        catch (Exception)
        {
            // handled by DownloadFileCompleted event
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
        _downloader.CancelAsync();
    }

    private void OnDownloadStarted(object? sender, DownloadStartedEventArgs args)
    {
        DownloadStartedEvents?.Invoke(sender, new DownloadStartedArgs(
            Url, args.FileName, FilePath, args.TotalBytesToReceive));
    }

    private void OnDownloadProgressed(object? sender, DownloadProgressChangedEventArgs args)
    {
        DownloadProgressedEvents?.Invoke(sender, new DownloadProgressedArgs(
            args.ReceivedBytesSize,
            args.TotalBytesToReceive,
            args.ProgressPercentage));
    }

    private void OnDownloadCompleted(object? sender, AsyncCompletedEventArgs args)
    {
        var success = args.Error == null && !args.Cancelled;
        DownloadCompletedEvents?.Invoke(sender, new DownloadCompletedArgs(success, FileName, FilePath, args.Error));
    }
}