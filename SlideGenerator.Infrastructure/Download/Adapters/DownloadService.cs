using System.Collections.Concurrent;
using System.ComponentModel;
using SlideGenerator.Application.Modules.Download.Models;
using IDownloadService = SlideGenerator.Application.Modules.Download.Abstractions.IDownloadService;

namespace SlideGenerator.Infrastructure.Download.Adapters;

/// <summary>
///     Adapts the external downloader service to the application download service abstraction.
/// </summary>
/// <param name="core">The core downloader service instance to be adapted.</param>
public class DownloadService(Downloader.DownloadService core) : IDownloadService
{
    /// <summary>
    ///     Stores the mapping between application-level download started event handlers and their corresponding core-level
    ///     handlers.
    /// </summary>
    private readonly
        ConcurrentDictionary<EventHandler<IDownloadStartedEventArgs>, EventHandler<Downloader.DownloadStartedEventArgs>>
        _downloadStartedHandlers = new();

    /// <inheritdoc />
    /// <summary>
    ///     Disposes the underlying core downloader service.
    /// </summary>
    public void Dispose()
    {
        core.Dispose();
    }

    /// <inheritdoc />
    /// <summary>
    ///     Asynchronously disposes the underlying core downloader service.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous disposal operation.</returns>
    public ValueTask DisposeAsync()
    {
        return core.DisposeAsync();
    }

    /// <inheritdoc />
    /// <summary>
    ///     Occurs when a file download operation has completed.
    /// </summary>
    public event EventHandler<AsyncCompletedEventArgs>? DownloadFileCompleted
    {
        add => core.DownloadFileCompleted += value;
        remove => core.DownloadFileCompleted -= value;
    }

    /// <inheritdoc />
    /// <summary>
    ///     Occurs when a download operation has started and the file information is available.
    /// </summary>
    public event EventHandler<IDownloadStartedEventArgs>? DownloadStarted
    {
        add
        {
            if (value is null) return;

            EventHandler<Downloader.DownloadStartedEventArgs> coreHandler = (sender, args) =>
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
    /// <summary>
    ///     Starts an asynchronous download of a file from the specified URL to the given local path.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="filePath">The local path where the file should be saved.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous download operation.</returns>
    public Task DownloadFileTaskAsync(string url, string filePath)
    {
        return core.DownloadFileTaskAsync(url, filePath);
    }

    /// <inheritdoc />
    /// <summary>
    ///     Pauses the current download operation.
    /// </summary>
    public void Pause()
    {
        core.Pause();
    }

    /// <inheritdoc />
    /// <summary>
    ///     Resumes the current download operation if it was paused.
    /// </summary>
    public void Resume()
    {
        core.Resume();
    }

    /// <inheritdoc />
    /// <summary>
    ///     Cancels the current download operation asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous cancellation operation.</returns>
    public Task CancelTaskAsync()
    {
        return core.CancelTaskAsync();
    }
}