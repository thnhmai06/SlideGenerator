using Downloader;
using TaoSlideTotNghiep.DTOs.Requests;

namespace TaoSlideTotNghiep.DTOs.Responses;

/// <summary>
/// Base download response.
/// </summary>
public abstract record DownloadResponse(string Url, string FilePath)
    : Response(RequestType.Download, true), IFilePathBased;

/// <summary>
/// Download progress update.
/// </summary>
public record DownloadStatusResponse(
    string Url,
    string FilePath,
    double Progress,
    long DownloadedBytes,
    long TotalBytes,
    DownloadStatus Status)
    : DownloadResponse(Url, FilePath);