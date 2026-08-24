/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Utilities
 * File: TextNormalization.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Globalization;
using System.Text;

namespace SlideGenerator.Utilities;

public static class TextNormalization
{
    /// <summary>
    ///     Normalizes a string for loose equality comparison: lowercase, Vietnamese diacritics and "đ"
    ///     stripped, whitespace/underscore/hyphen removed.
    /// </summary>
    /// <param name="value">The string to normalize.</param>
    /// <returns>The normalized string, or empty if <paramref name="value" /> is null/empty.</returns>
    public static string NormalizeForMatching(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var lowered = value.ToLowerInvariant().Replace('đ', 'd');
        var decomposed = lowered.Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (ch is ' ' or '_' or '-') continue;
            builder.Append(ch);
        }

        return builder.ToString();
    }
}
