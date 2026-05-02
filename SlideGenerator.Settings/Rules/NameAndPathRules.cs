namespace SlideGenerator.Settings.Rules;

/// <summary>
///     Defines naming conventions and constant values related to application settings.
/// </summary>
public static class NameAndPathRules
{
    /// <summary>The official application name.</summary>
    public const string AppName = "SlideGenerator";

    private static string UserPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), // Roaming
            AppName);

    public static string DefaultTempPath => Path.Combine(Path.GetTempPath(), AppName);

    public static class Setting
    {
        /// <summary>
        ///     The default base filename for the main settings file.
        /// </summary>
        private const string FileName = "Settings";

        public static string GetFilePath(string ext) => Path.Combine(UserPath, $"{FileName}{ext}");
    }

    public static class Job
    {
        private const string FileName = "Jobs";

        public static string DatabasePath => Path.Combine(UserPath, $"{FileName}.db");
    }
}