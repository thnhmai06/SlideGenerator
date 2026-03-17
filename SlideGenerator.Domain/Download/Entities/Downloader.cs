using Downloader;

namespace SlideGenerator.Domain.Download.Entities;

/// <summary>
///     Represents a downloadable file with download state management and result caching.
/// </summary>
/// <remarks>
///     This class encapsulates the download lifecycle:
///     1. Temporary file (.crdownload) is used during download
///     2. Upon completion, the file is renamed to final name with extension
///     The class must be disposed to clean up the underlying <see cref="Service" /> service.
/// </remarks>
/// Reviewed by @thnhmai06 at 04/03/2025 21:49:56 GMT+7
public sealed class Downloader : IDisposable
{
    private readonly Queue<string> _extension = new();

    public readonly DownloadInfo Info;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Downloader" /> class.
    /// </summary>
    /// <param name="info">The information of file wants to download.</param>
    /// <param name="config">Download configuration settings (timeout, retry, chunks, etc.).</param>
    internal Downloader(DownloadInfo info, DownloadConfiguration config)
    {
        Info = info;
        _extension.Enqueue(TempExtension);
        _extension.Enqueue(string.Empty);

        Service = new DownloadService(config);
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

    public static string TempExtension => ".crdownload";

    public string FilePath => Path.Combine(Info.SaveFolder, Info.FileName + _extension.Peek());

    /// <summary>
    ///     Gets the underlying downloader service for managing the download process.
    /// </summary>
    public DownloadService Service { get; }

    /// <summary>
    ///     Disposes the underlying downloader service and releases unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Service.Dispose();
    }

    /// <summary>
    ///     Downloads the file asynchronously.
    /// </summary>
    /// <remarks>
    ///     The download uses a temporary file (.crdownload extension) during transfer.
    ///     Upon successful completion, the file is renamed to its final name.
    /// </remarks>
    /// <returns>A task representing the asynchronous download operation.</returns>
    public async Task Download()
    {
        await Service.DownloadFileTaskAsync(Info.Url, FilePath).ConfigureAwait(false);
    }
}