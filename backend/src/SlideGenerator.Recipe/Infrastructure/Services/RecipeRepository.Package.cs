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
using SlideGenerator.Recipe.Domain.Models;
using SlideGenerator.Recipe.Domain.Models.Graphs;
using SlideGenerator.Recipe.Domain.Rules;

namespace SlideGenerator.Recipe.Infrastructure.Services;

internal sealed partial class RecipeRepository
{
    /// <inheritdoc />
    public async Task ExportAsync(int id, string outputPath, string? password, CancellationToken ct = default)
    {
        outputPath = Path.GetFullPath(outputPath);
        var entry = await GetAsync(id, ct).ConfigureAwait(false);
        var manifest = entry.Graph.GetReferencedFiles();

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await Task.Run(() =>
        {
            // Build Data file paths
            var workbookMapping =
                Export_BuildEntryMapping(manifest.Workbooks, RecipePackageRules.Data.Workbooks.FolderPrefix, ct);
            var presentationMapping =
                Export_BuildEntryMapping(manifest.Presentations, RecipePackageRules.Data.Presentations.FolderPrefix,
                    ct);
            var exportGraph = Export_BuildGraph(entry.Graph, workbookMapping, presentationMapping);

            using var outputStream = File.Create(outputPath);
            using var zipStream = new ZipOutputStream(outputStream);
            zipStream.SetLevel(9);
            if (!string.IsNullOrEmpty(password))
                zipStream.Password = password;

            // Graph file
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
        RecipeGraph importedGraph = new([], []);

        var workbooksDirectory = Path.GetFullPath(saveFolders.Workbooks);
        var presentationsDirectory = Path.GetFullPath(saveFolders.Presentations);

        await Task.Run(() =>
        {
            // Validation
            var compressedSize = new FileInfo(filePath).Length;
            if (compressedSize > RecipePackageRules.MaxCompressedArchiveBytes)
                throw new InvalidDataException(
                    $"Archive rejected: compressed size {compressedSize} exceeds limit " +
                    $"{RecipePackageRules.MaxCompressedArchiveBytes} bytes.");

            using var inputStream = File.OpenRead(filePath);
            using var zipFile = new ZipFile(inputStream);
            if (!string.IsNullOrEmpty(password))
                zipFile.Password = password;

            if (zipFile.Count > RecipePackageRules.MaxEntryCount)
                throw new InvalidDataException(
                    $"Archive rejected: entry count {zipFile.Count} exceeds limit " +
                    $"{RecipePackageRules.MaxEntryCount}.");

            // Graph file
            var graphJson = Import_ReadGraphFile(zipFile)
                            ?? throw new InvalidDataException(
                                $"Archive rejected: required entry '{RecipePackageRules.Data.RecipeFileName}' is missing.");
            try
            {
                importedGraph = JsonSerializer.Deserialize<RecipeGraph>(graphJson, GraphSerializerOptions)
                                ?? throw new InvalidDataException(
                                    $"Archive rejected: '{RecipePackageRules.Data.RecipeFileName}' deserialize as null.");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException(
                    $"Archive rejected: '{RecipePackageRules.Data.RecipeFileName}' contains invalid JSON.", ex);
            }

            // Data files
            var (wbMapping, pptMapping) = Import_BuildPathMappings(
                importedGraph, workbooksDirectory, presentationsDirectory);
            importedGraph = Import_ApplyPathMappings(importedGraph, wbMapping, pptMapping);
            Import_ExtractWithMappings(zipFile, wbMapping, pptMapping,
                workbooksDirectory + Path.DirectorySeparatorChar,
                presentationsDirectory + Path.DirectorySeparatorChar,
                ct);
        }, ct).ConfigureAwait(false);

        var metadata = await AddAsync(new RecipeInput(name, importedGraph), ct).ConfigureAwait(false);
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
        Import_BuildPathMappings(RecipeGraph graph, string workbooksDir, string presentationsDir)
    {
        var wbUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pptUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var wbMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pptMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in graph.Nodes)
            switch (node)
            {
                case WorkbookNode wn:
                {
                    var filename = Path.GetFileName(wn.Workbook.BookPath);
                    if (!string.IsNullOrEmpty(filename) && !wbMapping.ContainsKey(filename))
                        wbMapping[filename] = Import_ResolveTargetPath(filename, workbooksDir, wbUsed);
                    break;
                }
                case PresentationNode pn:
                {
                    var filename = Path.GetFileName(pn.Presentation.PresentationPath);
                    if (!string.IsNullOrEmpty(filename) && !pptMapping.ContainsKey(filename))
                        pptMapping[filename] = Import_ResolveTargetPath(filename, presentationsDir, pptUsed);
                    break;
                }
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
    ///     Returns a copy of <paramref name="graph" /> in which each <see cref="WorkbookNode" />
    ///     and <see cref="PresentationNode" /> has its path replaced by the deduplicated absolute
    ///     path from the corresponding mapping. Nodes whose filename is absent from the mapping are
    ///     left unchanged.
    /// </summary>
    private static RecipeGraph Import_ApplyPathMappings(
        RecipeGraph graph,
        Dictionary<string, string> workbookMapping,
        Dictionary<string, string> presentationMapping)
    {
        var fixedNodes = graph.Nodes.Select(node => node switch
        {
            WorkbookNode wn when workbookMapping.TryGetValue(
                    Path.GetFileName(wn.Workbook.BookPath), out var dest) =>
                wn with { Workbook = wn.Workbook with { BookPath = dest } },
            PresentationNode pn when presentationMapping.TryGetValue(
                    Path.GetFileName(pn.Presentation.PresentationPath), out var dest) =>
                pn with { Presentation = pn.Presentation with { PresentationPath = dest } },
            _ => node
        }).ToList();
        return graph with { Nodes = fixedNodes };
    }

    /// <summary>
    ///     Iterates all file entries in <paramref name="zipFile" />, enforces per-entry size limits,
    ///     and delegates each entry to <see cref="Import_ExtractSingleEntry" />.
    ///     Skips <c>Graph.json</c> and directory entries.
    /// </summary>
    private static void Import_ExtractWithMappings(
        ZipFile zipFile,
        Dictionary<string, string> workbookMapping,
        Dictionary<string, string> presentationMapping,
        string workbooksFull, string presentationsFull,
        CancellationToken ct)
    {
        var totalUncompressed = 0L;
        foreach (ZipEntry zipEntry in zipFile)
        {
            ct.ThrowIfCancellationRequested();

            if (!zipEntry.IsFile) continue;
            var entryName = zipEntry.Name;
            if (string.Equals(entryName, RecipePackageRules.Data.RecipeFileName,
                    StringComparison.OrdinalIgnoreCase)) continue;
            Import_EnforceEntrySizeLimits(zipEntry, ref totalUncompressed);
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
    /// <remarks>
    ///     Validates the entry's uncompressed size and compression ratio against
    ///     <see cref="RecipePackageRules" /> limits before reading to prevent OOM attacks.
    /// </remarks>
    private static string? Import_ReadGraphFile(ZipFile zipFile)
    {
        foreach (ZipEntry entry in zipFile)
        {
            if (!entry.IsFile) continue;
            if (!string.Equals(entry.Name, RecipePackageRules.Data.RecipeFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (entry.Size > RecipePackageRules.MaxGraphUncompressedBytes)
                throw new InvalidDataException(
                    $"Archive rejected: '{RecipePackageRules.Data.RecipeFileName}' uncompressed size {entry.Size} " +
                    $"exceeds limit {RecipePackageRules.MaxGraphUncompressedBytes}.");
            if (entry.CompressedSize > 0)
            {
                var ratio = (double)entry.Size / entry.CompressedSize;
                if (ratio > RecipePackageRules.MaxEntryCompressionRatio)
                    throw new InvalidDataException(
                        $"Archive rejected: '{RecipePackageRules.Data.RecipeFileName}' compression ratio {ratio:F1} " +
                        $"exceeds limit {RecipePackageRules.MaxEntryCompressionRatio:F1}.");
            }

            using var stream = zipFile.GetInputStream(entry);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        return null;
    }

    /// <summary>
    ///     Validates the uncompressed size and compression ratio of a single entry against
    ///     <see cref="RecipePackageRules" /> limits and accumulates the running total.
    ///     Throws <see cref="InvalidDataException" /> if any limit is exceeded.
    /// </summary>
    private static void Import_EnforceEntrySizeLimits(ZipEntry entry, ref long totalUncompressed)
    {
        if (entry.Size > RecipePackageRules.MaxEntryUncompressedBytes)
            throw new InvalidDataException(
                $"Archive rejected: entry '{entry.Name}' uncompressed size {entry.Size} exceeds limit " +
                $"{RecipePackageRules.MaxEntryUncompressedBytes}.");

        if (entry.CompressedSize > 0)
        {
            var ratio = (double)entry.Size / entry.CompressedSize;
            if (ratio > RecipePackageRules.MaxEntryCompressionRatio)
                throw new InvalidDataException(
                    $"Archive rejected: entry '{entry.Name}' compression ratio {ratio:F1} exceeds limit " +
                    $"{RecipePackageRules.MaxEntryCompressionRatio:F1}.");
        }

        totalUncompressed += Math.Max(0, entry.Size);
        if (totalUncompressed > RecipePackageRules.MaxTotalUncompressedBytes)
            throw new InvalidDataException(
                $"Archive rejected: total uncompressed size {totalUncompressed} exceeds limit " +
                $"{RecipePackageRules.MaxTotalUncompressedBytes}.");
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
    ///     Returns a copy of <paramref name="graph" /> in which every <see cref="WorkbookNode" />
    ///     and <see cref="PresentationNode" /> whose an absolute path appears in the entry mapping has
    ///     its path replaced by the plain filename (e.g. <c>data.xlsx</c>) suitable for storage
    ///     inside the zip archive. Nodes whose paths are not in the mapping are left unchanged.
    /// </summary>
    private static RecipeGraph Export_BuildGraph(
        RecipeGraph graph,
        ReadOnlyDictionary<string, string> workbookEntryMapping,
        ReadOnlyDictionary<string, string> presentationEntryMapping)
    {
        var exportNodes = graph.Nodes.Select(node => node switch
        {
            WorkbookNode wn when workbookEntryMapping.TryGetValue(wn.Workbook.BookPath, out var entry) =>
                wn with { Workbook = wn.Workbook with { BookPath = Path.GetFileName(entry) } },
            PresentationNode pn when presentationEntryMapping.TryGetValue(pn.Presentation.PresentationPath,
                    out var entry) =>
                pn with { Presentation = pn.Presentation with { PresentationPath = Path.GetFileName(entry) } },
            _ => node
        }).ToList();
        return graph with { Nodes = exportNodes };
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