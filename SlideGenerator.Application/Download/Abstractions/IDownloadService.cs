using System.ComponentModel;
using SlideGenerator.Application.Download.Models;

namespace SlideGenerator.Application.Download.Abstractions;

/// <summary>
///     Defines a contract for an asynchronous file downloading service.
/// </summary>
public interface IDownloadService : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     Occurs when a file download operation completes or is canceled.
    /// </summary>
    event EventHandler<AsyncCompletedEventArgs> DownloadFileCompleted;

    /// <summary>
    ///     Occurs when a file download operation begins.
    /// </summary>
    event EventHandler<IDownloadStartedEventArgs> DownloadStarted;

    /// <summary>
    ///     Starts an asynchronous file download task.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="filePath">The local file path where the downloaded content will be saved.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous download operation.</returns>
    Task DownloadFileTaskAsync(string url, string filePath);

    /// <summary>
    ///     Pauses the ongoing download operation.
    /// </summary>
    void Pause();

    /// <summary>
    ///     Resumes a paused download operation.
    /// </summary>
    void Resume();

    /// <summary>
    ///     Cancels the ongoing download operation asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous cancellation operation.</returns>
    Task CancelTaskAsync();
}
