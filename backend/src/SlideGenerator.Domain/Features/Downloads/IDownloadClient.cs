namespace SlideGenerator.Domain.Features.Downloads;

/// <summary>
///     Abstraction for downloading external resources.
/// </summary>
public interface IDownloadClient
{
    /// <summary>
    ///     Downloads a resource to the specified folder.
    /// </summary>
    Task<DownloadResult> DownloadAsync(Uri uri, DirectoryInfo saveFolder, CancellationToken cancellationToken);
}

/// <summary>
///     Result of a download operation.
/// </summary>
public sealed record DownloadResult(bool Success, string? FilePath, string? ErrorMessage);