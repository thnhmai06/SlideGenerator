namespace SlideGenerator.Application.Download.Abstractions;

public interface IDownloadStartedEventArgs
{
    /// <summary>
    /// Gets the total number of bytes in a System.Net.WebClient data download operation.
    /// </summary>
    /// <returns>A System.Int64 value that indicates the number of bytes that will be received.</returns>
    long TotalBytesToReceive { get; }

    /// <summary>
    /// Gets the name of the file which is being downloaded.
    /// </summary>
    string FileName { get; }
}