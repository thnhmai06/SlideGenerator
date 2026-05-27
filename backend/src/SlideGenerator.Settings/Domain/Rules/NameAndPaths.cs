/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: NameAndPaths.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using SlideGenerator.Cryptography.Application.Abstractions;
using SlideGenerator.Utilities.Helper;

namespace SlideGenerator.Settings.Domain.Rules;

/// <summary>
///     Defines naming conventions and constant values related to application settings.
/// </summary>
public static class NameAndPaths
{
    /// <summary>The official application name.</summary>
    private const string AppName = "SlideGenerator";

    /// <summary>
    ///     Gets the base path for user-specific application data.
    /// </summary>
    public static string UserPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), // AppData/Local
            AppName);

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
        Directory.CreateDirectory(LogsFolder.System);
        Directory.CreateDirectory(LogsFolder.Workflows);
    }

    /// <summary>
    ///     Provides predefined folder paths for storing system and workflow logs.
    /// </summary>
    public static class LogsFolder
    {
        /// <summary>
        ///     Represents the path to the folder designated for storing system logs.
        /// </summary>
        public static string System => Path.Combine(UserPath, "Logs", "System");

        /// <summary>
        ///     Represents the predefined folder path for storing workflow logs.
        /// </summary>
        public static string Workflows => Path.Combine(UserPath, "Logs", "Workflows");
    }

    /// <summary>
    ///     Gets the default assets directory path for the application.
    /// </summary>
    public static class AssetsFolder
    {
        private static string DefaultAssetsPath => Path.Combine(UserPath, "Assets");

        public static string GetDownloadDir(string? assetsPath, string bookPath, string sheetName, string colName,
            IHashPathRegistry registry)
        {
            assetsPath ??= DefaultAssetsPath;
            var bookName = Path.GetFileNameWithoutExtension(bookPath);
            var hash = registry.GetShortHash(bookPath);
            var bookFolder = $"{Normalization.SanitizeFileName(bookName)}_{hash}";
            sheetName = Normalization.SanitizeFileName(sheetName);
            colName = Normalization.SanitizeFileName(colName);
            return Path.Combine(assetsPath, bookFolder, sheetName, colName, "Download");
        }

        public static string GetEditDir(string? assetsPath, string bookPath, string sheetName, string colName,
            IHashPathRegistry registry)
        {
            assetsPath ??= DefaultAssetsPath;
            var bookName = Path.GetFileNameWithoutExtension(bookPath);
            var hash = registry.GetShortHash(bookPath);
            var bookFolder = $"{Normalization.SanitizeFileName(bookName)}_{hash}";
            sheetName = Normalization.SanitizeFileName(sheetName);
            colName = Normalization.SanitizeFileName(colName);
            return Path.Combine(assetsPath, bookFolder, sheetName, colName, "Edit");
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
        private static string FilePath => Path.Combine(UserPath, $"{FileName}.db");

        /// <summary>
        ///     Gets the SQLite connection string for the workflow persistence database.
        /// </summary>
        public static string ConnectionString => $"Data Source={FilePath}";
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
        private static string FilePath => Path.Combine(UserPath, $"{FileName}.db");

        /// <summary>
        ///     Gets the SQLite connection string for the recipe database.
        /// </summary>
        public static string ConnectionString => $"Data Source={FilePath}";
    }
}