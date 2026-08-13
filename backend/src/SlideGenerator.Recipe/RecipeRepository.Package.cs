/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RecipeRepository.Package.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace SlideGenerator.Recipe;

internal sealed partial class RecipeRepository
{
    /// <inheritdoc />
    public async Task ExportAsync(int id, string outputPath, string? password, CancellationToken ct = default)
    {
        outputPath = Path.GetFullPath(outputPath);
        var entry = await GetAsync(id, ct).ConfigureAwait(false);
        var workbookPaths = entry.Recipe.Mappings
            .SelectMany(m => m.Sources.Select(s => s.Workbook.BookPath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var presentationPaths = entry.Recipe.Mappings
            .Select(m => m.Template.Presentation.PresentationPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await Task.Run(() =>
        {
            // Build Data file paths
            var workbookMapping =
                Export_BuildEntryMapping(workbookPaths, RecipePackageRules.Data.Workbooks.FolderPrefix, ct);
            var presentationMapping =
                Export_BuildEntryMapping(presentationPaths, RecipePackageRules.Data.Presentations.FolderPrefix,
                    ct);
            var exportGraph = Export_BuildGraph(entry.Recipe, workbookMapping, presentationMapping);

            using var outputStream = File.Create(outputPath);
            using var zipStream = new ZipOutputStream(outputStream);
            zipStream.SetLevel(9);
            if (!string.IsNullOrEmpty(password))
                zipStream.Password = password;

            // Recipe file
            var graphBytes = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(exportGraph, GraphSerializerOptions));
            var graphEntry = new ZipEntry(RecipePackageRules.Data.RecipeFileName)
            {
                DateTime = DateTime.UtcNow,
                Size = graphBytes.Length
            };
            if (!string.IsNullOrEmpty(password))
                graphEntry.AESKeySize = 256;
            zipStream.PutNextEntry(graphEntry);
            zipStream.Write(graphBytes, 0, graphBytes.Length);
            zipStream.CloseEntry();

            // Data files
            Export_AddFilesFromMapping(zipStream, workbookMapping, password, ct);
            Export_AddFilesFromMapping(zipStream, presentationMapping, password, ct);

            zipStream.Finish();
        }, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IRecipeMetadata> ImportAsync(
        string filePath, string? password,
        (string Workbooks, string Presentations) saveFolders,
        CancellationToken ct = default)
    {
        filePath = Path.GetFullPath(filePath);
        var name = Path.GetFileNameWithoutExtension(filePath);
        Mappings.Recipe imported = new([]);

        var workbooksDirectory = Path.GetFullPath(saveFolders.Workbooks);
        var presentationsDirectory = Path.GetFullPath(saveFolders.Presentations);

        await Task.Run(() =>
        {
            using var inputStream = File.OpenRead(filePath);
            using var zipFile = new ZipFile(inputStream);
            if (!string.IsNullOrEmpty(password))
                zipFile.Password = password;

            // Recipe file
            var graphJson = Import_ReadGraphFile(zipFile)
                            ?? throw new InvalidDataException(
                                $"Archive rejected: required entry '{RecipePackageRules.Data.RecipeFileName}' is missing.");
            try
            {
                imported = JsonSerializer.Deserialize<Mappings.Recipe>(graphJson, GraphSerializerOptions)
                                ?? throw new InvalidDataException(
                                    $"Archive rejected: '{RecipePackageRules.Data.RecipeFileName}' deserialize as null.");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException(
                    $"Archive rejected: '{RecipePackageRules.Data.RecipeFileName}' contains invalid JSON.", ex);
            }

            // A syntactically valid but field-less recipe.json (e.g. "{}") deserializes with a null
            // Mappings — treat that as an empty recipe rather than crash downstream.
            imported = new Mappings.Recipe(Mappings: imported.Mappings ?? []);

            // Data files
            var (wbMapping, pptMapping) = Import_BuildPathMappings(
                imported, workbooksDirectory, presentationsDirectory);
            imported = Import_ApplyPathMappings(imported, wbMapping, pptMapping);
            Import_ExtractWithMappings(zipFile, wbMapping, pptMapping,
                workbooksDirectory + Path.DirectorySeparatorChar,
                presentationsDirectory + Path.DirectorySeparatorChar,
                ct);
        }, ct).ConfigureAwait(false);

        var metadata = await AddAsync(new RecipeInput(name, imported), ct).ConfigureAwait(false);
        return metadata;
    }

    #region Import Helpers

    /// <summary>
    ///     For each file node in <paramref name="graph" />, resolves a deduplicated absolute path
    ///     under the respective target folder. If a file with the same name already exists on disk
    ///     (or was already claimed within this batch), a <c>_N</c> suffix is appended.
    ///     Only the bare filename is used, so path-traversal values are silently sanitized.
    /// </summary>
    /// <returns>
    ///     Two dictionaries keyed by the bare filename stored in the zip (e.g. <c>"data.xlsx"</c>),
    ///     mapping to the deduplicated absolute path where the file will be extracted.
    /// </returns>
    private static (Dictionary<string, string> Workbooks, Dictionary<string, string> Presentations)
        Import_BuildPathMappings(Mappings.Recipe graph, string workbooksDir, string presentationsDir)
    {
        var wbUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pptUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var wbMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pptMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in graph.Mappings)
        {
            foreach (var source in mapping.Sources)
            {
                var filename = Path.GetFileName(source.Workbook.BookPath);
                if (!string.IsNullOrEmpty(filename) && !wbMapping.ContainsKey(filename))
                    wbMapping[filename] = Import_ResolveTargetPath(filename, workbooksDir, wbUsed);
            }

            var pptFilename = Path.GetFileName(mapping.Template.Presentation.PresentationPath);
            if (!string.IsNullOrEmpty(pptFilename) && !pptMapping.ContainsKey(pptFilename))
                pptMapping[pptFilename] = Import_ResolveTargetPath(pptFilename, presentationsDir, pptUsed);
        }

        return (wbMapping, pptMapping);
    }

    /// <summary>
    ///     Returns a deduplicated absolute path for <paramref name="filename" /> under
    ///     <paramref name="targetDir" />. If the direct path already exists on disk or is already
    ///     claimed in <paramref name="used" />, appends <c>_N</c> (N = 1, 2, …) until a free slot is found.
    /// </summary>
    private static string Import_ResolveTargetPath(string filename, string targetDir, HashSet<string> used)
    {
        var stem = Path.GetFileNameWithoutExtension(filename);
        var ext = Path.GetExtension(filename);

        var candidate = Path.GetFullPath(Path.Combine(targetDir, filename));
        if (!File.Exists(candidate) && used.Add(candidate))
            return candidate;

        for (var i = 1;; i++)
        {
            candidate = Path.GetFullPath(Path.Combine(targetDir, $"{stem}_{i}{ext}"));
            if (!File.Exists(candidate) && used.Add(candidate))
                return candidate;
        }
    }

    /// <summary>
    ///     Returns a copy of <paramref name="graph" /> in which each <see cref="Mappings.WorksheetSource" />
    ///     and <see cref="Mappings.Mapping.Template" /> has its path replaced by the deduplicated absolute
    ///     path from the corresponding mapping. Nodes whose filename is absent from the mapping are
    ///     left unchanged.
    /// </summary>
    private static Mappings.Recipe Import_ApplyPathMappings(
        Mappings.Recipe graph,
        Dictionary<string, string> workbookMapping,
        Dictionary<string, string> presentationMapping)
    {
        var fixedMappings = graph.Mappings.Select(m =>
        {
            var fixedSources = m.Sources.Select(s =>
                workbookMapping.TryGetValue(Path.GetFileName(s.Workbook.BookPath), out var dest)
                    ? s with { Workbook = s.Workbook with { BookPath = dest } }
                    : s).ToList();
            var fixedTemplate = presentationMapping.TryGetValue(
                Path.GetFileName(m.Template.Presentation.PresentationPath), out var pptDest)
                ? m.Template with { Presentation = m.Template.Presentation with { PresentationPath = pptDest } }
                : m.Template;
            return m with { Sources = fixedSources, Template = fixedTemplate };
        }).ToList();
        return new Mappings.Recipe(Mappings: fixedMappings);
    }

    /// <summary>
    ///     Iterates all file entries in <paramref name="zipFile" /> and delegates each entry to
    ///     <see cref="Import_ExtractSingleEntry" />. Skips <c>Recipe.json</c> and directory entries.
    /// </summary>
    private static void Import_ExtractWithMappings(
        ZipFile zipFile,
        Dictionary<string, string> workbookMapping,
        Dictionary<string, string> presentationMapping,
        string workbooksFull, string presentationsFull,
        CancellationToken ct)
    {
        foreach (ZipEntry zipEntry in zipFile)
        {
            ct.ThrowIfCancellationRequested();

            if (!zipEntry.IsFile) continue;
            var entryName = zipEntry.Name;
            if (string.Equals(entryName, RecipePackageRules.Data.RecipeFileName,
                    StringComparison.OrdinalIgnoreCase)) continue;
            Import_ExtractSingleEntry(zipFile, zipEntry, entryName,
                workbookMapping, presentationMapping, workbooksFull, presentationsFull, ct);
        }
    }

    /// <summary>
    ///     Extracts a single zip entry to the deduplicated path from the mapping, after validating:
    ///     the entry is under a known folder prefix (<c>Workbooks/</c> or <c>Presentations/</c>),
    ///     the extension is allowed, the reconstructed path stays within the target directory
    ///     (Zip Slip guard — throws), and the filename appears in the graph mapping (allowlist — skips).
    /// </summary>
    private static void Import_ExtractSingleEntry(
        ZipFile zipFile, ZipEntry zipEntry, string entryName,
        Dictionary<string, string> workbookMapping,
        Dictionary<string, string> presentationMapping,
        string workbooksFull, string presentationsFull,
        CancellationToken ct)
    {
        string targetDirFull;
        string relativeName;
        IReadOnlySet<string> allowedExtensions;
        Dictionary<string, string> mapping;

        if (entryName.StartsWith(RecipePackageRules.Data.Workbooks.FolderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativeName = entryName[RecipePackageRules.Data.Workbooks.FolderPrefix.Length..];
            targetDirFull = workbooksFull;
            allowedExtensions = RecipePackageRules.Data.Workbooks.FileExtensions;
            mapping = workbookMapping;
        }
        else if (entryName.StartsWith(RecipePackageRules.Data.Presentations.FolderPrefix,
                     StringComparison.OrdinalIgnoreCase))
        {
            relativeName = entryName[RecipePackageRules.Data.Presentations.FolderPrefix.Length..];
            targetDirFull = presentationsFull;
            allowedExtensions = RecipePackageRules.Data.Presentations.FileExtensions;
            mapping = presentationMapping;
        }
        else
        {
            return;
        }

        if (string.IsNullOrEmpty(relativeName)) return;
        var ext = Path.GetExtension(relativeName);
        if (!allowedExtensions.Contains(ext)) return;

        // Zip Slip guard: reconstruct path from zip entry name and verify it stays in-bounds.
        var reconstructed = Path.GetFullPath(Path.Combine(targetDirFull, relativeName));
        if (!reconstructed.StartsWith(targetDirFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(
                $"Archive rejected: entry '{entryName}' escapes the target directory.");

        // Allowlist check: filename must appear in the graph mapping.
        if (!mapping.TryGetValue(relativeName, out var dest)) return;

        var safeDirPath = Path.GetFullPath(Path.GetDirectoryName(dest)!);
        Directory.CreateDirectory(safeDirPath);
        ct.ThrowIfCancellationRequested();

        using var entryStream = zipFile.GetInputStream(zipEntry);
        using var targetStream = File.Create(dest);
        StreamUtils.Copy(entryStream, targetStream, new byte[4096]);
    }

    /// <summary>
    ///     Finds and returns the UTF-8 text of the graph file entry inside
    ///     <paramref name="zipFile" />, or <see langword="null" /> if the entry is absent.
    /// </summary>
    private static string? Import_ReadGraphFile(ZipFile zipFile)
    {
        foreach (ZipEntry entry in zipFile)
        {
            if (!entry.IsFile) continue;
            if (!string.Equals(entry.Name, RecipePackageRules.Data.RecipeFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            using var stream = zipFile.GetInputStream(entry);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        return null;
    }

    #endregion

    #region Export Helpers

    /// <summary>
    ///     Builds a mapping from each existing absolute file path to its zip entry name
    ///     (e.g. <c>C:\data.xlsx</c> → <c>Workbooks/data.xlsx</c>).
    ///     Uses stem-based deduplication via <see cref="Export_ResolveFileName" />;
    ///     files that do not exist on disk are skipped.
    /// </summary>
    private static ReadOnlyDictionary<string, string> Export_BuildEntryMapping(
        IReadOnlySet<string> filePaths, string folderPrefix, CancellationToken ct)
    {
        var usedStems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var usedOutputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var filePath in filePaths)
        {
            ct.ThrowIfCancellationRequested();

            if (!File.Exists(filePath)) continue;
            var relName = Export_ResolveFileName(filePath, usedStems, usedOutputNames);
            mapping[filePath] = folderPrefix + relName;
        }

        return mapping.AsReadOnly();
    }

    /// <summary>
    ///     Returns a copy of <paramref name="graph" /> in which every <see cref="Mappings.WorksheetSource" />
    ///     and <see cref="Mappings.Mapping.Template" /> whose an absolute path appears in the entry mapping has
    ///     its path replaced by the plain filename (e.g. <c>data.xlsx</c>) suitable for storage
    ///     inside the zip archive. Nodes whose paths are not in the mapping are left unchanged.
    /// </summary>
    private static Mappings.Recipe Export_BuildGraph(
        Mappings.Recipe graph,
        ReadOnlyDictionary<string, string> workbookEntryMapping,
        ReadOnlyDictionary<string, string> presentationEntryMapping)
    {
        var exportMappings = graph.Mappings.Select(m =>
        {
            var exportSources = m.Sources.Select(s =>
                workbookEntryMapping.TryGetValue(s.Workbook.BookPath, out var entry)
                    ? s with { Workbook = s.Workbook with { BookPath = Path.GetFileName(entry) } }
                    : s).ToList();
            var exportTemplate = presentationEntryMapping.TryGetValue(
                m.Template.Presentation.PresentationPath, out var pptEntry)
                ? m.Template with { Presentation = m.Template.Presentation with { PresentationPath = Path.GetFileName(pptEntry) } }
                : m.Template;
            return m with { Sources = exportSources, Template = exportTemplate };
        }).ToList();
        return new Mappings.Recipe(Mappings: exportMappings);
    }

    /// <summary>
    ///     Writes each file in <paramref name="entryMapping" /> to <paramref name="zipStream" />
    ///     using the pre-computed entry name as the zip path. Skips files that no longer exist on
    ///     disk. Applies AES-256 encryption when <paramref name="password" /> is non-empty.
    /// </summary>
    private static void Export_AddFilesFromMapping(
        ZipOutputStream zipStream,
        IReadOnlyDictionary<string, string> entryMapping,
        string? password,
        CancellationToken ct)
    {
        foreach (var (filePath, entryName) in entryMapping)
        {
            ct.ThrowIfCancellationRequested();

            if (!File.Exists(filePath)) continue;
            var fileInfo = new FileInfo(filePath);
            var zipEntry = new ZipEntry(entryName)
            {
                DateTime = fileInfo.LastWriteTimeUtc,
                Size = fileInfo.Length
            };
            if (!string.IsNullOrEmpty(password))
                zipEntry.AESKeySize = 256;
            zipStream.PutNextEntry(zipEntry);
            using var fileStream = File.OpenRead(filePath);
            StreamUtils.Copy(fileStream, zipStream, new byte[4096]);
            zipStream.CloseEntry();
        }
    }

    /// <summary>
    ///     Files sharing the same stem (regardless of extension) are treated as duplicates.
    ///     First occurrence keeps original name; the following occurrences get <c>_N</c> suffix.
    ///     Skips any candidate already taken as output (e.g., generated for another file).
    /// </summary>
    private static string Export_ResolveFileName(
        string filePath,
        Dictionary<string, int> usedStems,
        HashSet<string> usedOutputNames)
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

    #endregion
}
