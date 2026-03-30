using SlideGenerator.Application.Download.Abstractions;

namespace SlideGenerator.Infrastructure.Download.Adapters;

public sealed class DownloadStartedEventArgs(global::Downloader.DownloadStartedEventArgs core) : IDownloadStartedEventArgs
{
    public long TotalBytesToReceive { get; } = core.TotalBytesToReceive;
    public string FileName { get; } = core.FileName;
}