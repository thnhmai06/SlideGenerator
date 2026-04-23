using SlideGenerator.Application.Download.Models;

namespace SlideGenerator.Infrastructure.Download.Adapters;

/// <summary>
///     Provides event data for the
///     <see cref="SlideGenerator.Application.Download.Abstractions.IDownloadService.DownloadStarted" /> event.
/// </summary>
/// <param name="core">The original core event arguments containing download metadata.</param>
public sealed class DownloadStartedEventArgs(Downloader.DownloadStartedEventArgs core) : IDownloadStartedEventArgs
{
    /// <inheritdoc />
    /// <summary>
    ///     Gets the total number of bytes to be received during the download.
    /// </summary>
    public long TotalBytesToReceive { get; } = core.TotalBytesToReceive;

    /// <inheritdoc />
    /// <summary>
    ///     Gets the name of the file being downloaded.
    /// </summary>
    public string FileName { get; } = core.FileName;
}