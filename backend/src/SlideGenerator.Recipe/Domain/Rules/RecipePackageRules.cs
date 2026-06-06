/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RecipePackageRules.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;

namespace SlideGenerator.Recipe.Domain.Rules;

/// <summary>
///     Constraints applied when importing a <c>*.recipe</c> archive.
///     Prevent zip-bomb DoS, restrict extracted file types, and bound resource usage.
/// </summary>
public static class RecipePackageRules
{
    /// <summary>The package file extension.</summary>
    public const string PackageExtension = ".recipe";

    /// <summary>Maximum size of the package file on disk.</summary>
    public const long MaxCompressedArchiveBytes = 500L * 1024 * 1024; // 500 MB

    /// <summary>Maximum cumulative uncompressed size of all archive entries.</summary>
    public const long MaxTotalUncompressedBytes = 2L * 1024 * 1024 * 1024; // 2 GB

    /// <summary>Maximum uncompressed size of a single archive entry.</summary>
    public const long MaxEntryUncompressedBytes = 512L * 1024 * 1024; // 512 MB

    /// <summary>Maximum uncompressed size of the <c>Graph.json</c> entry.</summary>
    public const long MaxGraphUncompressedBytes = 16L * 1024 * 1024; // 16 MB

    /// <summary>Maximum number of entries allowed inside the archive.</summary>
    public const int MaxEntryCount = 1_000;

    /// <summary>
    ///     Maximum decompressed/compressed ratio for any single entry. Entries that exceed
    ///     this ratio indicate likely zip-bomb payloads.
    /// </summary>
    public const double MaxEntryCompressionRatio = 100.0;

    public static class Data
    {
        public static readonly string RecipeFileName = "Graph.json";

        public static class Workbooks
        {
            public const string FolderPrefix = "Workbooks/";

            /// <summary>
            ///     Allowed extensions for entries under <c>Workbooks/</c> (case-insensitive, with dot).
            ///     Derived from all <see cref="BookType" /> values.
            /// </summary>
            public static readonly IReadOnlySet<string> FileExtensions =
                new HashSet<string>(Enum.GetValues<BookType>().Select(t => t.ToExtension()),
                    StringComparer.OrdinalIgnoreCase);
        }

        public static class Presentations
        {
            public const string FolderPrefix = "Presentations/";

            /// <summary>
            ///     Allowed extensions for entries under <c>Presentations/</c> (case-insensitive, with dot).
            ///     Derived from all <see cref="PresentationType" /> values.
            /// </summary>
            public static readonly IReadOnlySet<string> FileExtensions =
                new HashSet<string>(Enum.GetValues<PresentationType>().Select(t => t.ToExtension()),
                    StringComparer.OrdinalIgnoreCase);
        }
    }
}