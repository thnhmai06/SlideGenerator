namespace SlideGenerator.Settings.Rules;

/// <summary>
///     Defines naming conventions and constant values related to application settings.
/// </summary>
public static class NameAndPathRules
{
    /// <summary>The official application name.</summary>
    public const string AppName = "SlideGenerator";

    /// <summary>
    ///     Gets the base path for user-specific application data.
    /// </summary>
    private static string UserPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), // Roaming
            AppName);

    /// <summary>
    ///     Gets the default temporary directory path for the application.
    /// </summary>
    public static string DefaultTempPath => Path.Combine(Path.GetTempPath(), AppName);

    /// <summary>
    ///     Contains naming rules for general application settings.
    /// </summary>
    public static class Setting
    {
        /// <summary>
        ///     The default base filename for the main settings file.
        /// </summary>
        private const string FileName = "Settings";

        /// <summary>
        ///     Calculates the full file path for the settings file with the specified extension.
        /// </summary>
        /// <param name="ext">The file extension to append.</param>
        /// <returns>The complete path to the settings file.</returns>
        public static string GetFilePath(string ext) => Path.Combine(UserPath, $"{FileName}{ext}");
    }

    /// <summary>
    ///     Contains naming rules for job-related data and storage.
    /// </summary>
    public static class Job
    {
        private const string FileName = "Jobs";

        /// <summary>
        ///     Gets the full path to the SQLite database used for job storage.
        /// </summary>
        public static string DatabasePath => Path.Combine(UserPath, $"{FileName}.db");
    }
}
