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
        string filePath, (string Workbooks, string Presentations, string Images) saveFolders,
        CancellationToken ct = default)
    {
        filePath = Path.GetFullPath(filePath);
        var name = Path.GetFileNameWithoutExtension(filePath);
        var workbooksDirectory = Path.GetFullPath(saveFolders.Workbooks);
        var presentationsDirectory = Path.GetFullPath(saveFolders.Presentations);
        var imagesDirectory = Path.GetFullPath(saveFolders.Images);

        var recipe = await Task.Run(() =>
        {
            using var inputStream = File.OpenRead(filePath);
            using var archive = new ZipArchive(inputStream, ZipArchiveMode.Read);

            var recipe = ReadRecipeFile(archive);
            var (workbookMapping, presentationMapping, imageMapping) = BuildPathMappings(
                recipe, workbooksDirectory, presentationsDirectory, imagesDirectory, GetImageEntryNames(archive));
            var relocatedRecipe = RewriteForImport(recipe, workbookMapping, presentationMapping, imageMapping);
            ExtractDataFiles(
                archive, workbookMapping, presentationMapping, imageMapping,
                workbooksDirectory, presentationsDirectory, imagesDirectory, ct);
            return relocatedRecipe;
        }, ct).ConfigureAwait(false);

        return await recipeRepository.AddAsync(new RecipeInput(name, recipe), ct).ConfigureAwait(false);
    }

    /// <summary>
    ///     Reads and deserializes the required <c>Recipe.json</c> entry from <paramref name="archive" />.
    ///     Every collection the payload leaves null or omits (mappings, sources, instructions, placeholders,
    ///     columns, shapes and ROI options) is normalized to an empty one rather than left to crash a later
    ///     consumer.
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
            // Runtime collections (including "mappings") can be null when the JSON omits or nulls them:
            // System.Text.Json does not enforce nullable annotations.
            return recipe.Normalized();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"Archive rejected: '{RecipePackageFormat.Data.Recipe.FileName}' contains invalid JSON.", ex);
        }
    }

    /// <summary>
    ///     Returns the relative names of every file entry under <c>Images/</c> in <paramref name="archive" />,
    ///     i.e. the fallback images the package actually ships.
    /// </summary>
    private static HashSet<string> GetImageEntryNames(ZipArchive archive)
    {
        var prefix = RecipePackageFormat.Data.Images.FolderPrefix;
        return archive.Entries
            .Where(e => !string.IsNullOrEmpty(e.Name) &&
                        e.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.FullName[prefix.Length..])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     For each file referenced by <paramref name="recipe" />, resolves a deduplicated absolute path
    ///     under the respective target folder. If a file with the same name already exists on disk (or was
    ///     already claimed within this batch), a <c>_N</c> suffix is appended. Only the bare filename is
    ///     used, so path-traversal values in the recipe itself are silently sanitized. Fallback images are
    ///     only mapped when the archive actually ships them (<paramref name="archivedImages" />), so a
    ///     recipe cannot make an import point at a file the package does not contain.
    /// </summary>
    /// <returns>
    ///     Three dictionaries keyed by the bare filename stored in the zip (e.g. <c>"data.xlsx"</c>), mapping
    ///     to the deduplicated absolute path where the file will be extracted.
    /// </returns>
    private static (Dictionary<string, string> Workbooks, Dictionary<string, string> Presentations,
        Dictionary<string, string> Images)
        BuildPathMappings(
            Models.Recipe recipe, string workbooksDirectory, string presentationsDirectory,
            string imagesDirectory, IReadOnlySet<string> archivedImages)
    {
        var usedWorkbookPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedPresentationPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedImagePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var workbookMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var presentationMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var imageMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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

            foreach (var instruction in mapping.ImageInstructions)
            {
                var imageFilename = Path.GetFileName(instruction.FallbackImagePath);
                if (!string.IsNullOrEmpty(imageFilename) &&
                    archivedImages.Contains(imageFilename) &&
                    !imageMapping.ContainsKey(imageFilename))
                    imageMapping[imageFilename] = ResolveImportTargetPath(
                        imageFilename, imagesDirectory, usedImagePaths);
            }
        }

        return (workbookMapping, presentationMapping, imageMapping);
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
    ///     Returns a copy of <paramref name="recipe" /> in which each <see cref="WorksheetSource" />,
    ///     <see cref="Mapping.Template" /> and <see cref="ImageInstruction" /> has its path replaced by the
    ///     deduplicated absolute path from the corresponding mapping. Workbook/presentation entries whose
    ///     filename is absent from the mapping are left unchanged; a fallback image is either mapped to a
    ///     file shipped inside the package or cleared — an imported recipe must never point at an arbitrary
    ///     local file.
    /// </summary>
    private static Models.Recipe RewriteForImport(
        Models.Recipe recipe,
        Dictionary<string, string> workbookMapping,
        Dictionary<string, string> presentationMapping,
        Dictionary<string, string> imageMapping)
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
            var images = m.ImageInstructions.Select(i =>
            {
                var filename = Path.GetFileName(i.FallbackImagePath);
                // An imported recipe may only point at images shipped inside the package: anything else is
                // cleared, so a crafted package cannot reference an arbitrary file on the importing machine.
                return !string.IsNullOrEmpty(filename) && imageMapping.TryGetValue(filename, out var imageDestination)
                    ? i with { FallbackImagePath = imageDestination }
                    : i with { FallbackImagePath = null };
            }).ToList();
            return m with { Sources = sources, Template = template, ImageInstructions = images };
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
        Dictionary<string, string> imageMapping,
        string workbooksDirectory, string presentationsDirectory, string imagesDirectory,
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
                zipEntry, entryName, workbookMapping, presentationMapping, imageMapping,
                workbooksDirectory, presentationsDirectory, imagesDirectory, ct);
        }
    }

    /// <summary>
    ///     Extracts a single zip entry to the deduplicated path from the mapping, after validating: the
    ///     entry is under a known folder prefix (<c>Workbooks/</c>, <c>Presentations/</c> or <c>Images/</c>),
    ///     the extension is allowed, the reconstructed path stays within the target directory (Zip Slip
    ///     guard — throws), and the filename appears in the mapping (allowlist — skips otherwise).
    /// </summary>
    private static void ExtractEntry(
        ZipArchiveEntry zipEntry, string entryName,
        Dictionary<string, string> workbookMapping,
        Dictionary<string, string> presentationMapping,
        Dictionary<string, string> imageMapping,
        string workbooksDirectory, string presentationsDirectory, string imagesDirectory,
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
        else if (entryName.StartsWith(RecipePackageFormat.Data.Images.FolderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativeName = entryName[RecipePackageFormat.Data.Images.FolderPrefix.Length..];
            directory = imagesDirectory;
            allowedExtensions = RecipePackageFormat.Data.Images.FileExtensions;
            mapping = imageMapping;
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