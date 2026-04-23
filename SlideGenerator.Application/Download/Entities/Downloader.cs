using SlideGenerator.Application.Download.Abstractions;
using SlideGenerator.Application.Download.Models;
using SlideGenerator.Application.Download.Rules;

namespace SlideGenerator.Application.Download.Entities;

/// <summary>
///     Represents a downloadable file with download state management and result caching.
/// </summary>
/// <remarks>
///     This class encapsulates the download lifecycle:
///     1. A temporary file (.crdownload) is used during the download process.
///     2. Upon completion, the file is renamed to its final name with the correct extension.
///     The class must be disposed to clean up the underlying <see cref="Service" />.
///     Reviewed by @thnhmai06 at 20/03/2026.
/// </remarks>
public sealed class Downloader : IDisposable, IAsyncDisposable
{
    private readonly Queue<string> _extension = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="Downloader" /> class.
    /// </summary>
    /// <param name="request">The download request containing URL and destination details.</param>
    internal Downloader(DownloadRequest request)
    {
        Request = request;

        _extension.Enqueue(FileExtensionRules.TempDownload);
        _extension.Enqueue(FileExtensionRules.NoExtensionDownload);

        Service.DownloadStarted += (_, e) =>
        {
            var ext = Path.GetExtension(e.FileName);
            _extension.Enqueue(ext);
        };
        Service.DownloadFileCompleted += (_, e) =>
        {
            if (e.Cancelled) return;

            var oldFilePath = FilePath;
            while (_extension.Count > 1)
                _extension.Dequeue();
            File.Move(oldFilePath, FilePath);
        };
    }

    /// <summary>
    ///     Gets the underlying downloader service used for managing the download process.
    /// </summary>
    private IDownloadService Service { get; } = null!;

    /// <summary>
    ///     Gets the original download request details.
    /// </summary>
    public DownloadRequest Request { get; }

    /// <summary>
    ///     Gets the current absolute path of the file on disk (which may include temporary extensions during download).
    /// </summary>
    public string FilePath => Path.Combine(Request.SaveFolder, Request.FileName + _extension.Peek());

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Service.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Service.Dispose();
    }

    /// <summary>
    ///     Occurs when the file download is successfully completed.
    /// </summary>
    public event EventHandler<object> DownloadFileCompleted
    {
        add => Service.DownloadFileCompleted += value;
        remove => Service.DownloadFileCompleted -= value;
    }

    /// <summary>
    ///     Starts the asynchronous download process.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    public async Task DownloadAsync()
    {
        await Service.DownloadFileTaskAsync(Request.Url, FilePath).ConfigureAwait(false);
    }

    /// <summary>
    ///     Pauses the ongoing download process.
    /// </summary>
    public void Pause()
    {
        Service.Pause();
    }

    /// <summary>
    ///     Resumes a previously paused download process.
    /// </summary>
    public void Resume()
    {
        Service.Resume();
    }

    /// <summary>
    ///     Cancels the ongoing download process.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the cancellation operation.</returns>
    public async Task Cancel()
    {
        await Service.CancelTaskAsync().ConfigureAwait(false);
    }
}