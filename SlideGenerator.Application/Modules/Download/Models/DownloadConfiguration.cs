using System.Net;

namespace SlideGenerator.Application.Modules.Download.Models;

/// <summary>
///     Contains all configuration settings required to initialize and manage a download process.
/// </summary>
public record DownloadConfiguration
{
    /// <summary>Gets or sets the timeout in milliseconds for reading data blocks.</summary>
    public int BlockTimeout;

    /// <summary>Gets or sets the initial number of chunks to divide the file into.</summary>
    public int ChunkCount;

    /// <summary>Gets or sets a value indicating whether the downloaded file should be deleted after processing.</summary>
    public bool DeleteAfterDownload;

    /// <summary>Gets or sets the speed limit for downloading in bytes per second.</summary>
    public int LimitBytesPerSecond;

    /// <summary>Gets or sets the maximum number of concurrent chunks allowed.</summary>
    public int MaxChunks;

    /// <summary>Gets or sets the absolute maximum bandwidth permitted in bytes per second.</summary>
    public int MaximumBytesPerSecond;

    /// <summary>Gets or sets the maximum number of retry attempts upon a connection failure.</summary>
    public int MaxTryAgainOnFailure;

    /// <summary>Gets or sets the web proxy to be used for the HTTP request, if any.</summary>
    public IWebProxy? Proxy;
}