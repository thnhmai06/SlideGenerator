using System.Collections.Concurrent;
using System.ComponentModel;
using SlideGenerator.Application.Download.Abstractions;
using IDownloadService = SlideGenerator.Application.Download.Abstractions.IDownloadService;

namespace SlideGenerator.Infrastructure.Download.Adapters;

/// <summary>
/// Adapts the external downloader service to the application download service abstraction.
/// </summary>
public class DownloadService(global::Downloader.DownloadService core) : IDownloadService
{
    private readonly
        ConcurrentDictionary<EventHandler<IDownloadStartedEventArgs>, EventHandler<global::Downloader.DownloadStartedEventArgs>>
        _downloadStartedHandlers = new();

    /// <inheritdoc />
    public void Dispose() => core.Dispose();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => core.DisposeAsync();

    /// <inheritdoc />
    public event EventHandler<AsyncCompletedEventArgs>? DownloadFileCompleted
    {
        add => core.DownloadFileCompleted += value;
        remove => core.DownloadFileCompleted -= value;
    }

    /// <inheritdoc />
    public event EventHandler<IDownloadStartedEventArgs>? DownloadStarted
    {
        add
        {
            if (value is null) return;

            EventHandler<global::Downloader.DownloadStartedEventArgs> coreHandler = (sender, args) =>
                value(sender, new DownloadStartedEventArgs(args));
            if (_downloadStartedHandlers.TryAdd(value, coreHandler)) core.DownloadStarted += coreHandler;
        }
        remove
        {
            if (value is null) return;

            if (_downloadStartedHandlers.TryRemove(value, out var coreHandler)) core.DownloadStarted -= coreHandler;
        }
    }

    /// <inheritdoc />
    public Task DownloadFileTaskAsync(string url, string filePath)
    {
        return core.DownloadFileTaskAsync(url, filePath);
    }

    /// <inheritdoc />
    public void Pause()
    {
        core.Pause();
    }

    /// <inheritdoc />
    public void Resume()
    {
        core.Resume();
    }

    /// <inheritdoc />
    public Task CancelTaskAsync()
    {
        return core.CancelTaskAsync();
    }
}