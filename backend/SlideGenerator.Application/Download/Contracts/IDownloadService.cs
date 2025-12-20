using SlideGenerator.Domain.Download.Interfaces;

namespace SlideGenerator.Application.Download.Contracts;

/// <summary>
///     Interface for download service.
/// </summary>
public interface IDownloadService
{
    /// <summary>Create image download task.</summary>
    /// <param name="url">The URL to download from.</param>
    /// <param name="saveFolder">The folder to save the downloaded file.</param>
    /// <returns>The created download task.</returns>
    IDownloadTask CreateImageTask(string url, DirectoryInfo saveFolder);

    public Task DownloadTask(IDownloadTask task);
}