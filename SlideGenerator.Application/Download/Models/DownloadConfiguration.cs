using System.Net;

namespace SlideGenerator.Application.Download.Models;

public record DownloadConfiguration
{
    public int BlockTimeout;
    public int ChunkCount;
    public bool DeleteAfterDownload;
    public int LimitBytesPerSecond;
    public int MaxChunks;
    public int MaximumBytesPerSecond;
    public int MaxTryAgainOnFailure;
    public IWebProxy? Proxy;
}