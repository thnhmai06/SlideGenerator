namespace SlideGenerator.Application.Download.Rules;

/// <summary>
///     Defines constant rules and default values for file extensions used during the download process.
/// </summary>
public static class FileExtensionRules
{
    /// <summary>
    ///     The temporary file extension used while a download is still in progress.
    /// </summary>
    public const string TempDownload = ".crdownload";

    /// <summary>
    ///     An empty string representing a file that has not yet had its final extension determined.
    /// </summary>
    public static readonly string NoExtensionDownload = string.Empty;
}
