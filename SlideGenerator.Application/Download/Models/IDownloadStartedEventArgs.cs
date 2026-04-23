namespace SlideGenerator.Application.Download.Models;

/// <summary>
///     Provides data for the event that is raised when a download successfully begins.
/// </summary>
public interface IDownloadStartedEventArgs
{
    /// <summary>
    ///     Gets the total number of bytes expected to be received.
    /// </summary>
    /// <returns>A <see cref="long" /> value indicating the total file size in bytes.</returns>
    long TotalBytesToReceive { get; }

    /// <summary>
    ///     Gets the name of the file being downloaded, usually extracted from the URL or headers.
    /// </summary>
    string FileName { get; }
}
