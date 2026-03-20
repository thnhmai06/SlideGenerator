using System.Net;

namespace SlideGenerator.Domain.Download.Models;

public record DownloadConfiguration
{
    public int MaximumBytesPerSecond;
    public int ChunkCount;
    public bool DeleteAfterDownload;
    public int LimitBytesPerSecond;
    public int MaxChunks;
    public int MaxTryAgainOnFailure;
    public int BlockTimeout;
    public IWebProxy? Proxy;
}