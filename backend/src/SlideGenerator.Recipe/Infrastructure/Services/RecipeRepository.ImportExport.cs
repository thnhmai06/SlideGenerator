/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RecipeRepository.ImportExport.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using SlideGenerator.Recipe.Domain.Models;
using SlideGenerator.Recipe.Domain.Rules;

namespace SlideGenerator.Recipe.Infrastructure.Services;

internal sealed partial class RecipeRepository
{
    /// <inheritdoc />
    public async Task ExportAsync(int id, string outputPath, string? password, CancellationToken ct = default)
    {
        outputPath = Path.GetFullPath(outputPath);
        var entry = await GetAsync(id, ct).ConfigureAwait(false);
        var manifest = Summarize(entry.Graph).GetReferencedFiles();

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await Task.Run(() =>
        {
            using var outputStream = File.Create(outputPath);
            using var zipStream = new ZipOutputStream(outputStream);
            zipStream.SetLevel(9);
            if (!string.IsNullOrEmpty(password))
                zipStream.Password = password;

            // Graph file
            var graphBytes = Encoding.UTF8.GetBytes(entry.Graph);
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
            ExportAddFiles(zipStream, manifest.Workbooks, RecipePackageRules.Data.Workbooks.FolderPrefix, password);
            ExportAddFiles(zipStream, manifest.Presentations, RecipePackageRules.Data.Presentations.FolderPrefix, password);

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
        string? graphJson = null;

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

            // Graph.json
            graphJson = ImportReadGraphJson(zipFile);
            if (graphJson == null)
                throw new InvalidDataException(
                    $"Archive rejected: required entry '{RecipePackageRules.Data.RecipeFileName}' is missing.");
            var summary = Summarize(graphJson);
            var manifest = summary.GetReferencedFiles();

            // Data files
            var workbooksDirectory = Path.GetFullPath(saveFolders.Workbooks) + Path.DirectorySeparatorChar;
            var presentationsDirectory = Path.GetFullPath(saveFolders.Presentations) + Path.DirectorySeparatorChar;
            ImportExtractZipEntries(zipFile, manifest, workbooksDirectory, presentationsDirectory);

            // TODO: Rewrite zip-relative file paths in graphJson to absolute paths.
            // Blocked on: recipe JSON schema finalization.
        }, ct).ConfigureAwait(false);

        var metadata = await AddAsync(new RecipeInput(name, graphJson!), ct).ConfigureAwait(false);
        return metadata;
    }

    #region Import Helpers

    private static void ImportExtractZipEntries(
        ZipFile zipFile, (IReadOnlySet<string> Workbooks, IReadOnlySet<string> Presentations) manifest,
        string workbooksFull, string presentationsFull)
    {
        var totalUncompressed = 0L;
        foreach (ZipEntry zipEntry in zipFile)
        {
            if (!zipEntry.IsFile) continue;
            var entryName = zipEntry.Name;
            if (string.Equals(entryName, RecipePackageRules.Data.RecipeFileName,
                    StringComparison.OrdinalIgnoreCase)) continue;
            ImportEnforceEntrySizeLimits(zipEntry, ref totalUncompressed);
            ImportExtractSingleEntry(zipFile, zipEntry, entryName, manifest, workbooksFull, presentationsFull);
        }
    }

    private static void ImportExtractSingleEntry(
        ZipFile zipFile, ZipEntry zipEntry, string entryName,
        (IReadOnlySet<string> Workbooks, IReadOnlySet<string> Presentations) manifest,
        string workbooksFull, string presentationsFull)
    {
        string targetDirFull;
        string relativeName;
        IReadOnlySet<string> allowedExtensions;
        IReadOnlySet<string> allowedFiles;

        if (entryName.StartsWith(RecipePackageRules.Data.Workbooks.FolderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativeName = entryName[RecipePackageRules.Data.Workbooks.FolderPrefix.Length..];
            targetDirFull = workbooksFull;
            allowedExtensions = RecipePackageRules.Data.Workbooks.FileExtensions;
            allowedFiles = manifest.Workbooks;
        }
        else if (entryName.StartsWith(RecipePackageRules.Data.Presentations.FolderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativeName = entryName[RecipePackageRules.Data.Presentations.FolderPrefix.Length..];
            targetDirFull = presentationsFull;
            allowedExtensions = RecipePackageRules.Data.Presentations.FileExtensions;
            allowedFiles = manifest.Presentations;
        }
        else
        {
            return;
        }

        if (string.IsNullOrEmpty(relativeName)) return;
        var ext = Path.GetExtension(relativeName);
        if (!allowedExtensions.Contains(ext)) return;
        if (!allowedFiles.Contains(ImportNormalizeManifestPath(entryName))) return;

        var targetPath = Path.Combine(targetDirFull, relativeName);
        var targetFull = Path.GetFullPath(targetPath);
        if (!targetFull.StartsWith(targetDirFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(
                $"Archive rejected: entry '{entryName}' escapes the target directory.");

        var safeDirPath = Path.GetFullPath(Path.GetDirectoryName(targetFull)!);
        Directory.CreateDirectory(safeDirPath);
        using var entryStream = zipFile.GetInputStream(zipEntry);
        using var targetStream = File.Create(targetFull);
        StreamUtils.Copy(entryStream, targetStream, new byte[4096]);
    }

    private static string? ImportReadGraphJson(ZipFile zipFile)
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

    private static void ImportEnforceEntrySizeLimits(ZipEntry entry, ref long totalUncompressed)
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

    private static string ImportNormalizeManifestPath(string entryName)
    {
        return entryName.Replace('\\', '/');
    }

    #endregion

    #region Export Helpers

    private static void ExportAddFiles(
        ZipOutputStream zipStream,
        IReadOnlySet<string> filePaths,
        string folderPrefix,
        string? password)
    {
        var usedStems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var usedOutputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath)) continue;
            var entryName = folderPrefix + ExportResolveFileName(filePath, usedStems, usedOutputNames);
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
    private static string ExportResolveFileName(
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

        for (var i = startCount; ; i++)
        {
            var candidate = $"{stem}_{i}{ext}";
            if (!usedOutputNames.Add(candidate)) continue;
            
            usedStems[stem] = i + 1;
            return candidate;
        }
    }

    #endregion
}
