/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: ZipImportRules.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Recipe.Domain.Rules;

/// <summary>
///     Constraints applied when importing a <c>*.recipe</c> archive.
///     Prevent zip-bomb DoS, restrict extracted file types, and bound resource usage.
/// </summary>
public static class ZipImportRules
{
    /// <summary>Maximum size of the input <c>*.recipe</c> file on disk.</summary>
    public const long MaxCompressedArchiveBytes = 500L * 1024 * 1024; // 500 MB

    /// <summary>Maximum cumulative uncompressed size of all archive entries.</summary>
    public const long MaxTotalUncompressedBytes = 4L * 1024 * 1024 * 1024; // 4 GB

    /// <summary>Maximum uncompressed size of a single archive entry.</summary>
    public const long MaxEntryUncompressedBytes = 512L * 1024 * 1024; // 512 MB

    /// <summary>Maximum number of entries allowed inside the archive.</summary>
    public const int MaxEntryCount = 50_000;

    /// <summary>
    ///     Maximum decompressed/compressed ratio for any single entry. Entries that exceed
    ///     this ratio indicate likely zip-bomb payloads.
    /// </summary>
    public const double MaxEntryCompressionRatio = 100.0;

    /// <summary>
    ///     Allowed extensions for entries under <c>Workbooks/</c> (case-insensitive, with dot).
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedWorkbookExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".xls", ".xlsx", ".xltx", ".ods", ".csv", ".tsv"
        };

    /// <summary>
    ///     Allowed extensions for entries under <c>Presentations/</c> (case-insensitive, with dot).
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedPresentationExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".potx", ".pptx", ".ppsx"
        };
}