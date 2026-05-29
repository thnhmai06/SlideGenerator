/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Utilities
 * File: Naming.cs
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

namespace SlideGenerator.Utilities.Helper;

public static class Naming
{
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
            normalized.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(normalized) ? @default : normalized;
    }
}