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
using SlideGenerator.Cryptography.Application.Abstractions;
using SlideGenerator.Utilities;

namespace SlideGenerator.Settings.Domain.Rules;

/// <summary>
///     Defines naming conventions and constant values related to application settings.
/// </summary>
public static class NameAndPaths
{
    /// <summary>The official application name.</summary>
    public const string AppName = "SlideGenerator";

    /// <summary>
    ///     Gets the base path for user-specific application data.
    /// </summary>
    private static string UserPath =>
        Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), // %LOCALAPPDATA%
            AppName));

    /// <summary>
    ///     Gets the base path for application-local data (executable directory).
    /// </summary>
    public static string BasePath => AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    ///     Ensures that all required application directories (data, logs, assets) exist on the disk.
    /// </summary>
    public static void InitializeDirectories()
    {
        Directory.CreateDirectory(UserPath);
        Directory.CreateDirectory(LogsFolder.SystemPath);
        Directory.CreateDirectory(LogsFolder.WorkflowPath);
    }

    public static class AppLocker
    {
        /// <summary>
        ///     Gets the name of the system-wide mutex used for single-instance detection.
        /// </summary>
        public static string MutexName => $"{AppName}-SingleInstance";

        /// <summary>
        ///     Gets the path to the single-instance PID lock file.
        /// </summary>
        public static string PidPath => Path.Combine(UserPath, $"{AppName}.pid");
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
    public static class AssetsFolder
    {
        private static string DefaultPath => Path.Combine(UserPath, "Assets");

        public static string GetDownloadDir(string? assetsPath, string bookPath, string sheetName, string colName,
            IHashPathRegistry registry)
        {
            assetsPath ??= DefaultPath;
            var bookName = Path.GetFileNameWithoutExtension(bookPath);
            var hash = registry.GetShortHash(bookPath);
            var bookFolder = $"{Naming.SanitizeFileName(bookName)}_{hash}";
            sheetName = Naming.SanitizeFileName(sheetName);
            colName = Naming.SanitizeFileName(colName);
            return Path.GetFullPath(Path.Combine(assetsPath, bookFolder, sheetName, colName, "Download"));
        }

        public static string GetEditDir(string? assetsPath, string bookPath, string sheetName, string colName,
            IHashPathRegistry registry)
        {
            assetsPath ??= DefaultPath;
            var bookName = Path.GetFileNameWithoutExtension(bookPath);
            var hash = registry.GetShortHash(bookPath);
            var bookFolder = $"{Naming.SanitizeFileName(bookName)}_{hash}";
            sheetName = Naming.SanitizeFileName(sheetName);
            colName = Naming.SanitizeFileName(colName);
            return Path.GetFullPath(Path.Combine(assetsPath, bookFolder, sheetName, colName, "Edit"));
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

    /// <summary>
    ///     Contains naming rules for the WorkflowCore SQLite persistence database.
    /// </summary>
    public static class WorkflowsFile
    {
        private const string FileName = "Workflows";

        /// <summary>
        ///     Gets the full path to the SQLite database used for workflow persistence.
        /// </summary>
        public static string FilePath => Path.Combine(UserPath, $"{FileName}.db");

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
        public static string FilePath => Path.Combine(UserPath, $"{FileName}.db");

        /// <summary>
        ///     Gets the SQLite connection string for the recipe database.
        /// </summary>
        public static string ConnectionString =>
            new SqliteConnectionStringBuilder { DataSource = FilePath }.ConnectionString;
    }
}