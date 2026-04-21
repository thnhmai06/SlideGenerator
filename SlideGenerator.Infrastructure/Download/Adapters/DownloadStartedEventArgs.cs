using SlideGenerator.Application.Download.Models;

namespace SlideGenerator.Infrastructure.Download.Adapters;

public sealed class DownloadStartedEventArgs(Downloader.DownloadStartedEventArgs core) : IDownloadStartedEventArgs
{
    public long TotalBytesToReceive { get; } = core.TotalBytesToReceive;
    public string FileName { get; } = core.FileName;
}