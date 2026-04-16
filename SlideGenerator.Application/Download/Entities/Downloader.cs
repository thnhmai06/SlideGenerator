using SlideGenerator.Application.Download.Abstractions;
using SlideGenerator.Domain.Download.Entities;
using SlideGenerator.Domain.Download.Models;
using SlideGenerator.Domain.Download.Rules;

namespace SlideGenerator.Application.Download.Entities;

/// <summary>
///     Represents a downloadable file with download state management and result caching.
/// </summary>
/// <remarks>
///     This class encapsulates the download lifecycle:
///     1. Temporary file (.crdownload) is used during download
///     2. Upon completion, the file is renamed to final name with extension
///     The class must be disposed to clean up the underlying <see cref="Service" /> service.
/// </remarks>
/// Reviewed by @thnhmai06 at 20/03/2026
public sealed class Downloader : IDownloader, IDisposable, IAsyncDisposable
{
    private readonly Queue<string> _extension = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="Downloader" /> class.
    /// </summary>
    /// <param name="request">The information of file wants to download.</param>
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
    ///     Gets the underlying downloader service for managing the download process.
    /// </summary>
    private IDownloadService Service { get; } = null!;

    public async ValueTask DisposeAsync()
    {
        await Service.DisposeAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        Service.Dispose();
    }

    public DownloadRequest Request { get; }

    public string FilePath => Path.Combine(Request.SaveFolder, Request.FileName + _extension.Peek());

    public event EventHandler<object> DownloadFileCompleted
    {
        add => Service.DownloadFileCompleted += value;
        remove => Service.DownloadFileCompleted -= value;
    }

    public async Task DownloadAsync()
    {
        await Service.DownloadFileTaskAsync(Request.Url, FilePath).ConfigureAwait(false);
    }

    public void Pause()
    {
        Service.Pause();
    }

    public void Resume()
    {
        Service.Resume();
    }

    public async Task Cancel()
    {
        await Service.CancelTaskAsync().ConfigureAwait(false);
    }
}