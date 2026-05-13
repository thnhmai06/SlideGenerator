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

using SlideGenerator.Common.Utilities;
using SlideGenerator.Cryptography.Application.Abstractions;

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
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), // AppData/Local
            AppName);

    // TODO: Log folder.

    /// <summary>
    ///     Gets the default assets directory path for the application.
    /// </summary>
    public static class AssetsFolder
    {
        private static string DefaultRootPath => Path.Combine(UserPath, "Assets");

        public static string GetDownloadDir(string? rootPath, string bookPath, string sheetName, string colName,
            IHashPathRegistry registry)
        {
            rootPath ??= DefaultRootPath;
            var bookName = Path.GetFileNameWithoutExtension(bookPath);
            var hash = registry.GetShortHash(bookPath);
            var bookFolder = $"{Normalization.NormalizeFileName(bookName)}_{hash}";
            sheetName = Normalization.NormalizeFileName(sheetName);
            colName = Normalization.NormalizeFileName(colName);
            return Path.Combine(rootPath, bookFolder, sheetName, colName, "Download");
        }

        public static string GetEditDir(string? rootPath, string bookPath, string sheetName, string colName,
            IHashPathRegistry registry)
        {
            rootPath ??= DefaultRootPath;
            var bookName = Path.GetFileNameWithoutExtension(bookPath);
            var hash = registry.GetShortHash(bookPath);
            var bookFolder = $"{Normalization.NormalizeFileName(bookName)}_{hash}";
            sheetName = Normalization.NormalizeFileName(sheetName);
            colName = Normalization.NormalizeFileName(colName);
            return Path.Combine(rootPath, bookFolder, sheetName, colName, "Edit");
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
        private const string FileName = "Setting";

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
        public static string ConnectionString => $"Data Source={FilePath}";
    }
}