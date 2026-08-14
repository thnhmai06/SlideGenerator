/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RecipePackageService.Export.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Frozen;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using SlideGenerator.Recipe.Formats;
using SlideGenerator.Recipe.Models;

namespace SlideGenerator.Recipe.Services;

internal sealed partial class RecipePackageService
{
    /// <inheritdoc cref="IRecipePackageService.ExportAsync" />
    public async Task ExportAsync(int id, string outputPath, CancellationToken ct = default)
    {
        outputPath = Path.GetFullPath(outputPath);
        var entry = await recipeRepository.GetAsync(id, ct).ConfigureAwait(false);

        var workbookPaths = entry.Recipe.Mappings
            .SelectMany(m => m.Sources.Select(s => s.Workbook.BookPath))
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        var presentationPaths = entry.Recipe.Mappings
            .Select(m => m.Template.Presentation.PresentationPath)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory)) Directory.CreateDirectory(outputDirectory);

        await Task.Run(() =>
        {
            var workbookMapping = BuildEntryMapping(
                workbookPaths, RecipePackageFormat.Data.Workbooks.FolderPrefix, ct);
            var presentationMapping = BuildEntryMapping(
                presentationPaths, RecipePackageFormat.Data.Presentations.FolderPrefix, ct);
            var portableRecipe = RewriteForExport(entry.Recipe, workbookMapping, presentationMapping);

            using var outputStream = File.Create(outputPath);
            using var archive = new ZipArchive(outputStream, ZipArchiveMode.Create);

            WriteRecipeFile(archive, portableRecipe);
            WriteDataFiles(archive, workbookMapping, ct);
            WriteDataFiles(archive, presentationMapping, ct);
        }, ct).ConfigureAwait(false);
    }

    /// <summary>
    ///     Writes the <c>Recipe.json</c> entry, serializing <paramref name="recipe" /> (already rewritten to
    ///     bare filenames by <see cref="RewriteForExport" />).
    /// </summary>
    private static void WriteRecipeFile(ZipArchive archive, Models.Recipe recipe)
    {
        var recipeBytes = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(recipe, RecipePackageFormat.Data.Recipe.Format));
        var recipeEntry = archive.CreateEntry(RecipePackageFormat.Data.Recipe.FileName, CompressionLevel.Optimal);
        using var entryStream = recipeEntry.Open();
        entryStream.Write(recipeBytes, 0, recipeBytes.Length);
    }

    /// <summary>
    ///     Builds a mapping from each existing absolute file path to its zip entry name
    ///     (e.g. <c>C:\data.xlsx</c> → <c>Workbooks/data.xlsx</c>). Uses stem-based deduplication via
    ///     <see cref="ResolveExportFileName" />; files that no longer exist on disk are skipped.
    /// </summary>
    private static FrozenDictionary<string, string> BuildEntryMapping(
        IReadOnlySet<string> filePaths, string folderPrefix, CancellationToken ct)
    {
        var usedStems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var usedOutputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var filePath in filePaths)
        {
            ct.ThrowIfCancellationRequested();

            if (!File.Exists(filePath)) continue;
            var entryName = ResolveExportFileName(filePath, usedStems, usedOutputNames);
            mapping[filePath] = folderPrefix + entryName;
        }

        return mapping.ToFrozenDictionary();
    }

    /// <summary>
    ///     Files sharing the same stem (regardless of extension) are treated as duplicates. The first
    ///     occurrence keeps the original name; later occurrences get a <c>_N</c> suffix. Skips any candidate
    ///     already taken as output (e.g., generated for another file).
    /// </summary>
    private static string ResolveExportFileName(
        string filePath, Dictionary<string, int> usedStems, HashSet<string> usedOutputNames)
    {
        var stem = Path.GetFileNameWithoutExtension(filePath);
        var ext = Path.GetExtension(filePath);

        if (!usedStems.TryGetValue(stem, out var startCount))
        {
            var original = stem + ext;
            if (usedOutputNames.Add(original))
            {
                usedStems[stem] = 1;
                return original;
            }

            startCount = 1;
            usedStems[stem] = startCount;
        }

        for (var i = startCount;; i++)
        {
            var candidate = $"{stem}_{i}{ext}";
            if (!usedOutputNames.Add(candidate)) continue;

            usedStems[stem] = i + 1;
            return candidate;
        }
    }

    /// <summary>
    ///     Returns a copy of <paramref name="recipe" /> in which every <see cref="WorksheetSource" /> and
    ///     <see cref="Mapping.Template" /> whose absolute path appears in <paramref name="workbookMapping" />
    ///     / <paramref name="presentationMapping" /> has its path replaced by the bare filename it will be
    ///     stored under in the zip (e.g. <c>data.xlsx</c>). Paths not in either mapping are left unchanged.
    /// </summary>
    private static Models.Recipe RewriteForExport(
        Models.Recipe recipe,
        FrozenDictionary<string, string> workbookMapping,
        FrozenDictionary<string, string> presentationMapping)
    {
        var rewritten = recipe.Mappings.Select(m =>
        {
            var sources = m.Sources.Select(s =>
                workbookMapping.TryGetValue(s.Workbook.BookPath, out var entryName)
                    ? s with { Workbook = s.Workbook with { BookPath = Path.GetFileName(entryName) } }
                    : s).ToList();
            var template = presentationMapping.TryGetValue(
                m.Template.Presentation.PresentationPath, out var pptEntryName)
                ? m.Template with
                {
                    Presentation = m.Template.Presentation with { PresentationPath = Path.GetFileName(pptEntryName) }
                }
                : m.Template;
            return m with { Sources = sources, Template = template };
        }).ToList();
        return new Models.Recipe(rewritten);
    }

    /// <summary>
    ///     Writes each file in <paramref name="entryMapping" /> to <paramref name="archive" /> using the
    ///     pre-computed entry name as the zip path. Skips files that no longer exist on disk.
    /// </summary>
    private static void WriteDataFiles(
        ZipArchive archive, FrozenDictionary<string, string> entryMapping, CancellationToken ct)
    {
        foreach (var (filePath, entryName) in entryMapping)
        {
            ct.ThrowIfCancellationRequested();

            if (!File.Exists(filePath)) continue;
            var zipEntry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var fileStream = File.OpenRead(filePath);
            using var entryStream = zipEntry.Open();
            fileStream.CopyTo(entryStream);
        }
    }
}