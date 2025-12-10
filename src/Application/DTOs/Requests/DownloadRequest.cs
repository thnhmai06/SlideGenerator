using System.Text.Json.Serialization;

namespace Application.DTOs.Requests;

#region Enums

/// <summary>
/// Types of download requests.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DownloadRequestType
{
    Start,
    Pause,
    Resume,
    Stop,
    Status
}

#endregion

#region Records

/// <summary>
/// Base download request.
/// </summary>
public abstract record DownloadRequest(DownloadRequestType Type, string FilePath) : Request(RequestType.Download),
    IFilePathBased;

/// <summary>
/// Request to start a new download.
/// </summary>
public record StartDownloadRequest(string Url, string FilePath) : DownloadRequest(DownloadRequestType.Start, FilePath);

/// <summary>
/// Request to pause a download.
/// </summary>
public record PauseDownloadRequest(string FilePath) : DownloadRequest(DownloadRequestType.Pause, FilePath);

/// <summary>
/// Request to resume a paused download.
/// </summary>
public record ResumeDownloadRequest(string FilePath) : DownloadRequest(DownloadRequestType.Resume, FilePath);

/// <summary>
/// Request to stop a download.
/// </summary>
public record StopDownloadRequest(string FilePath) : DownloadRequest(DownloadRequestType.Stop, FilePath);

/// <summary>
/// Request to get the status of a download.
/// </summary>
public record StatusDownloadRequest(string FilePath) : DownloadRequest(DownloadRequestType.Status, FilePath);

#endregion