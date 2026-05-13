/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Common
 * File: Normalization.cs
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

namespace SlideGenerator.Common.Utilities;

public static class Normalization
{
    /// <summary>
    ///     Normalizes a raw string value into a valid URI.
    /// </summary>
    /// <param name="value">The raw string value (e.g., from a spreadsheet cell).</param>
    /// <returns>A normalized <see cref="Uri" />, or <see langword="null" /> if the value is invalid or not a link.</returns>
    public static Uri? NormalizeUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        if (!trimmed.Contains("://")) trimmed = "https://" + trimmed;

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ? uri : null;
    }

    /// <summary>
    ///     Normalizes a file name by replacing invalid characters with underscores.
    /// </summary>
    /// <param name="value">The original string to normalize.</param>
    /// <param name="defaultValue">The default value to return if the input is null, empty, or whitespace.</param>
    /// <returns>A safe string suitable for use as a file or directory name.</returns>
    public static string NormalizeFileName(string? value, string? defaultValue = null)
    {
        defaultValue ??= string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        var normalized = value.Trim();
        normalized = Path.GetInvalidFileNameChars()
            .Aggregate(normalized, (current, invalid) => current.Replace(invalid, '_'));

        return string.IsNullOrWhiteSpace(normalized) ? defaultValue : normalized;
    }
}