/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RecipePackageService.Import.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using SlideGenerator.Recipe.Formats;
using SlideGenerator.Recipe.Models;

namespace SlideGenerator.Recipe.Services;

internal sealed partial class RecipePackageService
{
    /// <inheritdoc cref="IRecipePackageService.ImportAsync" />
    public async Task<IRecipeMetadata> ImportAsync(
        string filePath, (string Workbooks, string Presentations) saveFolders,
        CancellationToken ct = default)
    {
        filePath = Path.GetFullPath(filePath);
        var name = Path.GetFileNameWithoutExtension(filePath);
        var workbooksDirectory = Path.GetFullPath(saveFolders.Workbooks);
        var presentationsDirectory = Path.GetFullPath(saveFolders.Presentations);

        var recipe = await Task.Run(() =>
        {
            using var inputStream = File.OpenRead(filePath);
            using var archive = new ZipArchive(inputStream, ZipArchiveMode.Read);

            var recipe = ReadRecipeFile(archive);
            var (workbookMapping, presentationMapping) = BuildPathMappings(
                recipe, workbooksDirectory, presentationsDirectory);
            var relocatedRecipe = RewriteForImport(recipe, workbookMapping, presentationMapping);
            ExtractDataFiles(
                archive, workbookMapping, presentationMapping, workbooksDirectory, presentationsDirectory, ct);
            return relocatedRecipe;
        }, ct).ConfigureAwait(false);

        return await recipeRepository.AddAsync(new RecipeInput(name, recipe), ct).ConfigureAwait(false);
    }

    /// <summary>
    ///     Reads and deserializes the required <c>Recipe.json</c> entry from <paramref name="archive" />.
    ///     A <c>null</c> <c>Mappings</c> array (e.g., a crafted or truncated <c>"{}"</c> payload) is normalized
    ///     to an empty list rather than left to crash a later consumer.
    /// </summary>
    private static Models.Recipe ReadRecipeFile(ZipArchive archive)
    {
        var entry = archive.Entries.FirstOrDefault(e =>
            !string.IsNullOrEmpty(e.Name) &&
            string.Equals(e.FullName, RecipePackageFormat.Data.Recipe.FileName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            throw new InvalidDataException(
                $"Archive rejected: required entry '{RecipePackageFormat.Data.Recipe.FileName}' is missing.");

        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var json = reader.ReadToEnd();

        try
        {
            var recipe = JsonSerializer.Deserialize<Models.Recipe>(json, RecipePackageFormat.Data.Recipe.Format)
                         ?? throw new InvalidDataException(
                             $"Archive rejected: '{RecipePackageFormat.Data.Recipe.FileName}' deserialize as null.");
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            // recipe.Mappings can be null on runtime when "mappings":null
            return new Models.Recipe(recipe.Mappings ?? []);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"Archive rejected: '{RecipePackageFormat.Data.Recipe.FileName}' contains invalid JSON.", ex);
        }
    }

    /// <summary>
    ///     For each file referenced by <paramref name="recipe" />, resolves a deduplicated absolute path
    ///     under the respective target folder. If a file with the same name already exists on disk (or was
    ///     already claimed within this batch), a <c>_N</c> suffix is appended. Only the bare filename is
    ///     used, so path-traversal values in the recipe itself are silently sanitized.
    /// </summary>
    /// <returns>
    ///     Two dictionaries keyed by the bare filename stored in the zip (e.g. <c>"data.xlsx"</c>), mapping
    ///     to the deduplicated absolute path where the file will be extracted.
    /// </returns>
    private static (Dictionary<string, string> Workbooks, Dictionary<string, string> Presentations)
        BuildPathMappings(Models.Recipe recipe, string workbooksDirectory, string presentationsDirectory)
    {
        var usedWorkbookPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedPresentationPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var workbookMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var presentationMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in recipe.Mappings)
        {
            foreach (var source in mapping.Sources)
            {
                var filename = Path.GetFileName(source.Workbook.BookPath);
                if (!string.IsNullOrEmpty(filename) && !workbookMapping.ContainsKey(filename))
                    workbookMapping[filename] =
                        ResolveImportTargetPath(filename, workbooksDirectory, usedWorkbookPaths);
            }

            var presentationFilename = Path.GetFileName(mapping.Template.Presentation.PresentationPath);
            if (!string.IsNullOrEmpty(presentationFilename) && !presentationMapping.ContainsKey(presentationFilename))
                presentationMapping[presentationFilename] = ResolveImportTargetPath(
                    presentationFilename, presentationsDirectory, usedPresentationPaths);
        }

        return (workbookMapping, presentationMapping);
    }

    /// <summary>
    ///     Returns a deduplicated absolute path for <paramref name="filename" /> under
    ///     <paramref name="targetDirectory" />. If the direct path already exists on disk or is already
    ///     claimed in <paramref name="used" />, appends <c>_N</c> (N = 1, 2, …) until a free slot is found.
    ///     Unlike <see cref="ResolveExportFileName" /> (which dedup by stem, ignoring extension, since it
    ///     never touches disk), this dedup by the full filename against what's actually on disk — an
    ///     import must never silently overwrite an existing file.
    /// </summary>
    private static string ResolveImportTargetPath(string filename, string targetDirectory, HashSet<string> used)
    {
        var stem = Path.GetFileNameWithoutExtension(filename);
        var ext = Path.GetExtension(filename);

        var candidate = Path.GetFullPath(Path.Combine(targetDirectory, filename));
        if (!File.Exists(candidate) && used.Add(candidate))
            return candidate;

        for (var i = 1;; i++)
        {
            candidate = Path.GetFullPath(Path.Combine(targetDirectory, $"{stem}_{i}{ext}"));
            if (!File.Exists(candidate) && used.Add(candidate))
                return candidate;
        }
    }

    /// <summary>
    ///     Returns a copy of <paramref name="recipe" /> in which each <see cref="WorksheetSource" /> and
    ///     <see cref="Mapping.Template" /> has its path replaced by the deduplicated absolute path from the
    ///     corresponding mapping. Entries whose filename is absent from the mapping are left unchanged.
    /// </summary>
    private static Models.Recipe RewriteForImport(
        Models.Recipe recipe,
        Dictionary<string, string> workbookMapping,
        Dictionary<string, string> presentationMapping)
    {
        var rewritten = recipe.Mappings.Select(m =>
        {
            var sources = m.Sources.Select(s =>
                workbookMapping.TryGetValue(Path.GetFileName(s.Workbook.BookPath), out var destination)
                    ? s with { Workbook = s.Workbook with { BookPath = destination } }
                    : s).ToList();
            var template = presentationMapping.TryGetValue(
                Path.GetFileName(m.Template.Presentation.PresentationPath), out var pptDestination)
                ? m.Template with { Presentation = m.Template.Presentation with { PresentationPath = pptDestination } }
                : m.Template;
            return m with { Sources = sources, Template = template };
        }).ToList();
        return new Models.Recipe(rewritten);
    }

    /// <summary>
    ///     Iterates all file entries in <paramref name="archive" /> and delegates each entry to
    ///     <see cref="ExtractEntry" />. Skips <c>Recipe.json</c> and directory entries.
    /// </summary>
    private static void ExtractDataFiles(
        ZipArchive archive,
        Dictionary<string, string> workbookMapping,
        Dictionary<string, string> presentationMapping,
        string workbooksDirectory, string presentationsDirectory,
        CancellationToken ct)
    {
        foreach (var zipEntry in archive.Entries)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(zipEntry.Name)) continue; // directory entry
            var entryName = zipEntry.FullName;
            if (string.Equals(entryName, RecipePackageFormat.Data.Recipe.FileName, StringComparison.OrdinalIgnoreCase))
                continue;
            ExtractEntry(
                zipEntry, entryName, workbookMapping, presentationMapping,
                workbooksDirectory, presentationsDirectory, ct);
        }
    }

    /// <summary>
    ///     Extracts a single zip entry to the deduplicated path from the mapping, after validating: the
    ///     entry is under a known folder prefix (<c>Workbooks/</c> or <c>Presentations/</c>), the extension
    ///     is allowed, the reconstructed path stays within the target directory (Zip Slip guard — throws),
    ///     and the filename appears in the mapping (allowlist — skips otherwise).
    /// </summary>
    private static void ExtractEntry(
        ZipArchiveEntry zipEntry, string entryName,
        Dictionary<string, string> workbookMapping,
        Dictionary<string, string> presentationMapping,
        string workbooksDirectory, string presentationsDirectory,
        CancellationToken ct)
    {
        string directory;
        string relativeName;
        IReadOnlySet<string> allowedExtensions;
        Dictionary<string, string> mapping;

        if (entryName.StartsWith(RecipePackageFormat.Data.Workbooks.FolderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativeName = entryName[RecipePackageFormat.Data.Workbooks.FolderPrefix.Length..];
            directory = workbooksDirectory;
            allowedExtensions = RecipePackageFormat.Data.Workbooks.FileExtensions;
            mapping = workbookMapping;
        }
        else if (entryName.StartsWith(
                     RecipePackageFormat.Data.Presentations.FolderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativeName = entryName[RecipePackageFormat.Data.Presentations.FolderPrefix.Length..];
            directory = presentationsDirectory;
            allowedExtensions = RecipePackageFormat.Data.Presentations.FileExtensions;
            mapping = presentationMapping;
        }
        else
        {
            return; // unknown folder — not part of the package format, ignore
        }

        if (string.IsNullOrEmpty(relativeName)) return;
        if (!allowedExtensions.Contains(Path.GetExtension(relativeName))) return;

        // Zip Slip guard: reconstruct the path from the zip entry name and verify it stays in-bounds.
        // A plain StartsWith prefix check is unsafe (e.g. "...\Workbooks" would prefix-match
        // "...\WorkbooksEvil\") — GetRelativePath is the robust way to ask "is this still inside?"
        var reconstructed = Path.GetFullPath(Path.Combine(directory, relativeName));
        var relativeToTarget = Path.GetRelativePath(directory, reconstructed);
        if (relativeToTarget.StartsWith("..") || Path.IsPathRooted(relativeToTarget))
            throw new InvalidDataException($"Archive rejected: entry '{entryName}' escapes the target directory.");

        // Allowlist check: filename must appear in the recipe's own mapping.
        if (!mapping.TryGetValue(relativeName, out var destination)) return;

        Directory.CreateDirectory(Path.GetFullPath(Path.GetDirectoryName(destination)!));
        ct.ThrowIfCancellationRequested();

        using var entryStream = zipEntry.Open();
        using var targetStream = File.Create(destination);
        entryStream.CopyTo(targetStream);
    }
}