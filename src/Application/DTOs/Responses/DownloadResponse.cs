using Application.DTOs.Requests;
using Downloader;

namespace Application.DTOs.Responses;

#region Success

/// <summary>
/// Base download response.
/// </summary>
public abstract record DownloadSuccess(string FilePath, DownloadRequestType Type)
    : SuccessResponse(RequestType.Download),
        IFilePathBased;

/// <summary>
/// Response for starting a download.
/// </summary>
public record DownloadStartSuccess(string FilePath) : DownloadSuccess(FilePath, DownloadRequestType.Start);

/// <summary>
/// Response for pausing a download.
/// </summary>
public record DownloadPauseSuccess(string FilePath) : DownloadSuccess(FilePath, DownloadRequestType.Pause);

/// <summary>
/// Response for resuming a download.
/// </summary>
public record DownloadResumeSuccess(string FilePath) : DownloadSuccess(FilePath, DownloadRequestType.Resume);

/// <summary>
/// Response for stopping a download.
/// </summary>
public record DownloadStopSuccess(string FilePath) : DownloadSuccess(FilePath, DownloadRequestType.Stop);

/// <summary>
/// Download progress/status update.
/// </summary>
public record DownloadStatusSuccess(
    string Url,
    string FilePath,
    double Progress,
    long DownloadedBytes,
    long TotalBytes,
    DownloadStatus Status)
    : DownloadSuccess(FilePath, DownloadRequestType.Status);

#endregion

#region Error

public record DownloadError : ErrorResponse,
    IDownloadDto
{
    public string FilePath { get; init; }

    public DownloadError(string filePath, Exception exception)
        : base(RequestType.Download, exception)
    {
        FilePath = filePath;
    }
}

#endregion