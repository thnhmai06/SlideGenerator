/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RecipePackageFormat.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text.Json;
using System.Text.Json.Serialization;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;

namespace SlideGenerator.Recipe.Formats;

/// <summary>
///     Constraints applied when importing a <c>*.recipe</c> archive.
///     Restricts extracted file types and enforces the Zip Slip path guard.
/// </summary>
public static class RecipePackageFormat
{
    /// <summary>The package file extension.</summary>
    public const string PackageExtension = ".recipe";

    public static class Data
    {
        public static class Recipe
        {
            public const string FileName = "Recipe.json";

            public static readonly JsonSerializerOptions Format = BuildFormat();

            private static JsonSerializerOptions BuildFormat()
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                options.Converters.Add(new JsonStringEnumConverter());
                options.Converters.Add(new ReadOnlySetJsonConverterFactory());
                return options;
            }
        }

        public static class Workbooks
        {
            public const string FolderPrefix = "Workbooks/";

            /// <summary>
            ///     Allowed extensions for entries under <c>Workbooks/</c> (case-insensitive, with dot).
            ///     Derived from all <see cref="WorkbookType" /> values.
            /// </summary>
            public static readonly IReadOnlySet<string> FileExtensions =
                new HashSet<string>(Enum.GetValues<WorkbookType>().Select(t => t.ToExtension()),
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