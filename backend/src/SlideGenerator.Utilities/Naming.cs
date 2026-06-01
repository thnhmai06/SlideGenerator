/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Utilities
 * File: Naming.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Utilities;

public static class Naming
{
    // Union with Windows-specific chars so sanitization is consistent across platforms.
    private static readonly char[] InvalidFileNameChars =
        Path.GetInvalidFileNameChars()
            .Union(['"', '<', '>', '|', ':', '*', '?', '\\', '/'])
            .ToArray();

    /// <summary>
    ///     Normalizes a file name by replacing invalid characters with underscores.
    /// </summary>
    /// <param name="value">The original string to normalize.</param>
    /// <param name="default">The default value to return if the input is null, empty, or whitespace.</param>
    /// <returns>A safe string suitable for use as a file or directory name.</returns>
    public static string SanitizeFileName(string? value, string? @default = null)
    {
        @default ??= string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return @default;

        var normalized = value.Trim();
        normalized = string.Join("_",
            normalized.Split(InvalidFileNameChars, StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(normalized) ? @default : normalized;
    }
}