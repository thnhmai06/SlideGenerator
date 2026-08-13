/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: NameAndPaths.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Data.Sqlite;

namespace SlideGenerator.Settings.Immutable;

/// <summary>
///     Defines naming conventions and constant values related to application settings.
/// </summary>
public static class NameAndPaths
{
    /// <summary>
    ///     Gets the base path for application-local data (executable directory).
    /// </summary>
    public static string BasePath => Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);

    /// <summary>
    ///     Gets the base path for user-specific application data.
    ///     Returns <see cref="BasePath" /> when running in portable mode.
    /// </summary>
    private static string UserPath =>
        IsPortable()
            ? BasePath
            : Path.GetFullPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), // %LOCALAPPDATA%
                Application.Name));

    /// <summary>
    ///     Gets whether the application is running in portable mode (<c>--portable</c> flag).
    ///     When <see langword="true" />, all user data is stored relative to the executable directory.
    /// </summary>
    private static bool IsPortable()
    {
        return Environment.GetCommandLineArgs().Contains("--portable", StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Ensures that all required application directories (data, logs, assets) exist on the disk.
    /// </summary>
    public static void InitializeDirectories()
    {
        Directory.CreateDirectory(UserPath);

        // Logs
        Directory.CreateDirectory(LogsFolder.SystemPath);
        Directory.CreateDirectory(LogsFolder.WorkflowPath);

        // Temp (shared download cache)
        Directory.CreateDirectory(TempFolder.RootPath);

        // Data
        Directory.CreateDirectory(DataFolder.FolderPath);
    }

    /// <summary>
    ///     Provides metadata and identification constants for the application.
    /// </summary>
    public static class Application
    {
        /// <summary>The official application name.</summary>
        public const string Name = "SlideGenerator";

        /// <summary>The ASCII art representation of the application name.</summary>
        public const string NameArt =
            """
              /$$$$$$  /$$ /$$       /$$            /$$$$$$                                                     /$$                        
             /$$__  $$| $$|__/      | $$           /$$__  $$                                                   | $$                        
            | $$  \__/| $$ /$$  /$$$$$$$  /$$$$$$ | $$  \__/  /$$$$$$  /$$$$$$$   /$$$$$$   /$$$$$$  /$$$$$$  /$$$$$$    /$$$$$$   /$$$$$$ 
            |  $$$$$$ | $$| $$ /$$__  $$ /$$__  $$| $$ /$$$$ /$$__  $$| $$__  $$ /$$__  $$ /$$__  $$|____  $$|_  $$_/   /$$__  $$ /$$__  $$
             \____  $$| $$| $$| $$  | $$| $$$$$$$$| $$|_  $$| $$$$$$$$| $$  \ $$| $$$$$$$$| $$  \__/ /$$$$$$$  | $$    | $$  \ $$| $$  \__/
             /$$  \ $$| $$| $$| $$  | $$| $$_____/| $$  \ $$| $$_____/| $$  | $$| $$_____/| $$      /$$__  $$  | $$ /$$| $$  | $$| $$      
            |  $$$$$$/| $$| $$|  $$$$$$$|  $$$$$$$|  $$$$$$/|  $$$$$$$| $$  | $$|  $$$$$$$| $$     |  $$$$$$$  |  $$$$/|  $$$$$$/| $$      
             \______/ |__/|__/ \_______/ \_______/ \______/  \_______/|__/  |__/ \_______/|__/      \_______/   \___/   \______/ |__/      
            """;

        /// <summary>The application deployment type.</summary>
        public const string Type = "Backend sidecar";

        /// <summary>The official application repository URL.</summary>
        public const string Repository = "https://github.com/thnhmai06/SlideGenerator";

        /// <summary>The official application author.</summary>
        public const string Author = "Thành Mai (thnhmai06)";

        /// <summary>The license under which the application is distributed.</summary>
        public const string License = "Apache-2.0";
    }

    /// <summary>
    ///     Provides naming conventions for application instance locking mechanisms.
    /// </summary>
    public static class AppLocker
    {
        /// <summary>
        ///     Gets the name of the system-wide mutex used for single-instance detection.
        /// </summary>
        public const string MutexName = $"{Application.Name}-Instance";

        /// <summary>
        ///     Gets the path to the single-instance PID lock file.
        /// </summary>
        public static string PidPath => Path.Combine(UserPath, "Instance.pid");
    }

    /// <summary>
    ///     Provides predefined folder paths for storing system and workflow logs.
    /// </summary>
    public static class LogsFolder
    {
        /// <summary>
        ///     Represents the path to the folder designated for storing system logs.
        /// </summary>
        public static string SystemPath => Path.Combine(UserPath, "Logs", "System");

        /// <summary>
        ///     Represents the predefined folder path for storing workflow logs.
        /// </summary>
        public static string WorkflowPath => Path.Combine(UserPath, "Logs", "Workflows");
    }

    /// <summary>
    ///     Provides the shared, app-wide temp directory used to cache downloaded images that are
    ///     reused across multiple rows/jobs before being deleted once no longer referenced.
    /// </summary>
    public static class TempFolder
    {
        /// <summary>
        ///     Gets the root directory (under the OS temp folder) where cached downloads are stored.
        /// </summary>
        public static string RootPath => Path.Combine(Path.GetTempPath(), Application.Name);
    }

    /// <summary>
    ///     Provides predefined folder paths and connection settings for application data storage.
    /// </summary>
    public static class DataFolder
    {
        /// <summary>
        ///     Gets the root directory path for application data files.
        /// </summary>
        public static string FolderPath => Path.Combine(UserPath, "Data");

        /// <summary>
        ///     Contains naming rules for the single shared SQLite database (<c>Recipes</c>/<c>Requests</c>/
        ///     <c>Jobs</c> tables) — replaces the old per-purpose <c>Workflows.db</c>/<c>Recipes.db</c>/
        ///     <c>Cache.db</c>/<c>Studio.db</c> split.
        /// </summary>
        public static class DataFile
        {
            private const string FileName = "Data";

            /// <summary>
            ///     Gets the full path to the shared SQLite database.
            /// </summary>
            public static string FilePath => Path.Combine(FolderPath, $"{FileName}.db");

            /// <summary>
            ///     Gets the SQLite connection string for the shared database.
            /// </summary>
            public static readonly string ConnectionString =
                new SqliteConnectionStringBuilder { DataSource = FilePath }.ConnectionString;
        }

    }

    /// <summary>
    ///     Contains naming rules for general application settings.
    /// </summary>
    public static class SettingsFile
    {
        /// <summary>
        ///     The default base filename for the main settings file.
        /// </summary>
        private const string FileName = "appsettings";

        /// <summary>
        ///     Calculates the full file path for the settings file with the specified extension.
        /// </summary>
        /// <param name="ext">The file extension to append.</param>
        /// <returns>The complete path to the settings file.</returns>
        public static string GetFilePath(string ext)
        {
            return Path.Combine(UserPath, $"{FileName}{ext}");
        }
    }
}