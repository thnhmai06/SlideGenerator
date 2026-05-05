namespace SlideGenerator.Logging;

/// <summary>
///     Defines filesystem paths used by the logging module.
/// </summary>
public static class LoggingPaths
{
    /// <summary>
    ///     Gets the folder path where system log files are written.
    /// </summary>
    public static string LogFolderPath => Path.Combine(AppContext.BaseDirectory, "Logs");
}
