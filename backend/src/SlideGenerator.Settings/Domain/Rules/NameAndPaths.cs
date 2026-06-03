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
using SlideGenerator.Utilities;

namespace SlideGenerator.Settings.Domain.Rules;

/// <summary>
///     Defines naming conventions and constant values related to application settings.
/// </summary>
public static class NameAndPaths
{
    private const int PathHashLength = 7;

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

        // Assets
        Directory.CreateDirectory(AssetsFolder.DefaultFolder);

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
        public static string MutexName => $"{Application.Name}-Instance";

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
    ///     Gets the default assets directory path for the application.
    /// </summary>
    /// <summary>
    ///     Provides predefined folder paths and utilities for managing application assets.
    /// </summary>
    public static class AssetsFolder
    {
        /// <summary>
        ///     Gets the default directory path where application assets are stored.
        /// </summary>
        public static string DefaultFolder => Path.Combine(UserPath, "Assets");

        /// <summary>
        ///     Returns the download directory for a specific book/sheet/column combination.
        /// </summary>
        public static string GetDownloadDir(string? assetsPath, string bookPath, string sheetName, string colName)
        {
            assetsPath ??= DefaultFolder;
            var bookName = Path.GetFileNameWithoutExtension(bookPath);
            var hash = bookPath.HashText(PathHashLength);
            var bookFolder = $"{Naming.SanitizeFileName(bookName)}_{hash}";
            sheetName = Naming.SanitizeFileName(sheetName);
            colName = Naming.SanitizeFileName(colName);
            return Path.GetFullPath(Path.Combine(assetsPath, bookFolder, sheetName, colName, "Download"));
        }

        /// <summary>
        ///     Returns the edit directory for a specific book/sheet/column combination.
        /// </summary>
        public static string GetEditDir(string? assetsPath, string bookPath, string sheetName, string colName)
        {
            assetsPath ??= DefaultFolder;
            var bookName = Path.GetFileNameWithoutExtension(bookPath);
            var hash = bookPath.HashText(PathHashLength);
            var bookFolder = $"{Naming.SanitizeFileName(bookName)}_{hash}";
            sheetName = Naming.SanitizeFileName(sheetName);
            colName = Naming.SanitizeFileName(colName);
            return Path.GetFullPath(Path.Combine(assetsPath, bookFolder, sheetName, colName, "Edit"));
        }
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
        ///     Contains naming rules for the WorkflowCore SQLite persistence database.
        /// </summary>
        public static class WorkflowsFile
        {
            private const string FileName = "Workflows";

            /// <summary>
            ///     Gets the full path to the SQLite database used for workflow persistence.
            /// </summary>
            public static string FilePath => Path.Combine(FolderPath, $"{FileName}.db");

            /// <summary>
            ///     Gets the SQLite connection string for the workflow persistence database.
            /// </summary>
            public static string ConnectionString =>
                new SqliteConnectionStringBuilder { DataSource = FilePath }.ConnectionString;
        }

        /// <summary>
        ///     Contains naming rules for the recipe SQLite database.
        /// </summary>
        public static class RecipesFile
        {
            private const string FileName = "Recipes";

            /// <summary>
            ///     Gets the full path to the SQLite database used for recipe storage.
            /// </summary>
            public static string FilePath => Path.Combine(FolderPath, $"{FileName}.db");

            /// <summary>
            ///     Gets the SQLite connection string for the recipe database.
            /// </summary>
            public static string ConnectionString =>
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
        private const string FileName = "Settings";

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