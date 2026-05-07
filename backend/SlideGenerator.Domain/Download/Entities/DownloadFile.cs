using Downloader;

namespace SlideGenerator.Domain.Download.Entities;

/// <summary>
///     Represents a downloadable file with download state management and result caching.
/// </summary>
/// <remarks>
///     This class encapsulates the download lifecycle:
///     1. Temporary file (.crdownload) is used during download
///     2. Upon completion, the file is renamed to final name with extension
///     3. File bytes are cached in <see cref="Result" /> property
///     The class must be disposed to clean up the underlying <see cref="Downloader" /> service.
/// </remarks>
/// Reviewed by @thnhmai06 at 04/03/2025 21:49:56 GMT+7
public sealed class DownloadFile : IDisposable
{
    /// <summary>
    ///     Temporary file name (with .crdownload extension) used during download.
    /// </summary>
    private readonly string _tempFileWithExtensions;

    /// <summary>
    ///     The folder path where the file will be saved.
    /// </summary>
    public readonly string SaveFolder;

    /// <summary>
    ///     Gets the remote URL of the file to download.
    /// </summary>
    public readonly string Url;

    /// <summary>
    ///     Final file name (with actual extension) after download completion.
    /// </summary>
    private string _fileNameWithExtensions;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DownloadFile" /> class.
    /// </summary>
    /// <param name="url">The remote URL of the file to download.</param>
    /// <param name="saveFolder">The folder path where the file will be saved.</param>
    /// <param name="config">Download configuration settings (timeout, retry, chunks, etc.).</param>
    /// <param name="fileName">
    ///     Optional custom file name without extension. If null, the server-provided name is used.
    /// </param>
    public DownloadFile(string url, string saveFolder, DownloadConfiguration config, string? fileName = null)
    {
        Url = url;
        SaveFolder = saveFolder;
        _tempFileWithExtensions = (fileName ?? Guid.NewGuid().ToString()) + ".crdownload";
        _fileNameWithExtensions = _tempFileWithExtensions + ".bin";

        Downloader = new DownloadService(config);
        Downloader.DownloadStarted += (_, e) =>
        {
            var ext = Path.GetExtension(e.FileName);
            _fileNameWithExtensions = string.IsNullOrEmpty(fileName) ? e.FileName : fileName + ext;
        };
        Downloader.DownloadFileCompleted += (_, _) =>
        {
            if (File.Exists(TempFilePath))
                File.Move(TempFilePath, FilePath);
            Result = File.ReadAllBytes(FilePath);
        };
    }

    /// <summary>
    ///     Gets the full path to the temporary download file.
    /// </summary>
    private string TempFilePath => Path.Combine(SaveFolder, _tempFileWithExtensions);

    /// <summary>
    ///     Gets the full path to the final downloaded file.
    ///     The file may be non-existent until the download completes successfully.
    /// </summary>
    public string FilePath => Path.Combine(SaveFolder, _fileNameWithExtensions);

    /// <summary>
    ///     Gets the underlying downloader service for managing the download process.
    /// </summary>
    public DownloadService Downloader { get; }

    /// <summary>
    ///     Gets the downloaded file content as byte array.
    ///     Empty until <see cref="Download" /> completes successfully.
    /// </summary>
    public byte[] Result { get; private set; } = [];

    /// <summary>
    ///     Disposes the underlying downloader service and releases unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Downloader.Dispose();
    }

    /// <summary>
    ///     Downloads the file asynchronously from <see cref="Url" /> to the configured save folder.
    /// </summary>
    /// <remarks>
    ///     The download uses a temporary file (.crdownload extension) during transfer.
    ///     Upon successful completion, the file is renamed to its final name and
    ///     <see cref="Result" /> is populated with the file bytes.
    /// </remarks>
    /// <returns>A task representing the asynchronous download operation.</returns>
    public async Task Download()
    {
        await Downloader.DownloadFileTaskAsync(Url, TempFilePath).ConfigureAwait(false);
    }
}