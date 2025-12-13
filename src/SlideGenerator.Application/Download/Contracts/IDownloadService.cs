using SlideGenerator.Domain.Download.Interfaces;

namespace SlideGenerator.Application.Download.Contracts;

/// <summary>
/// Interface for download service.
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// Creates and starts a download task with event callbacks.
    /// </summary>
    /// <param name="url">The URL to download from.</param>
    /// <param name="savePath">The path to save the downloaded file.</param>
    /// <param name="httpClient">Optional HttpClient for URL correction.</param>
    /// <param name="onProgress">Callback for progress updates.</param>
    /// <param name="onCompleted">Callback when download completes.</param>
    /// <param name="onError">Callback when an error occurs.</param>
    /// <returns>The created download task.</returns>
    Task<IDownloadTask> CreateAndStartDownloadAsync(
        string url,
        string savePath,
        HttpClient httpClient,
        Action<IDownloadTask, double> onProgress,
        Action<IDownloadTask> onCompleted,
        Action<IDownloadTask, Exception> onError);
}