using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using SlideGenerator.Application.Modules.Download.Entities;
using SlideGenerator.Application.Modules.Download.Models;
using SlideGenerator.Application.Modules.Download.Rules;

namespace SlideGenerator.Application.Modules.Download.Services;

/// <summary>
///     Maintains a central registry of all active and completed downloads to prevent duplicate processing.
/// </summary>
public sealed class DownloadRegistry
{
    /// <summary>
    ///     The dictionary of active and completed downloads, keyed by the download request.
    /// </summary>
    private readonly ConcurrentDictionary<DownloadRequest, Downloader> _registry = new();

    /// <summary>Gets a read-only view of the currently active download registry.</summary>
    public IReadOnlyDictionary<DownloadRequest, Downloader> RegistryView => _registry;

    /// <summary>
    ///     Checks if a requested file has already been completely downloaded to the target directory.
    /// </summary>
    /// <param name="request">The download request details.</param>
    /// <param name="filePath">
    ///     When this method returns <see langword="true" />, contains the absolute path to the completed
    ///     file.
    /// </param>
    /// <returns><see langword="true" /> if the file exists and is not a temporary file; otherwise, <see langword="false" />.</returns>
    public static bool TryGetCompletedDownloadFilePath(DownloadRequest request,
        [MaybeNullWhen(false)] out string filePath)
    {
        var files = Directory.GetFiles(request.SaveFolder, $"{request.FileName}.*");
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file);
            if (ext.Equals(FileExtensionRules.TempDownload, StringComparison.OrdinalIgnoreCase)) continue;
            filePath = file;
            return true;
        }

        filePath = null;
        return false;
    }

    /// <summary>
    ///     Attempts to get an existing downloader for the request, or creates a new one if it doesn't exist and hasn't been
    ///     completed.
    /// </summary>
    /// <param name="request">The download request details.</param>
    /// <param name="config">The download configuration to apply if a new downloader is created.</param>
    /// <param name="downloader">
    ///     When this method returns, contains the <see cref="Downloader" /> instance, or
    ///     <see langword="null" /> if the file was already completed.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> if a new or existing downloader is returned; <see langword="false" /> if the download
    ///     is already finished.
    /// </returns>
    public bool TryGetOrCreateDownloader(DownloadRequest request, DownloadConfiguration? config,
        [MaybeNullWhen(false)] out Downloader downloader)
    {
        if (TryGetCompletedDownloadFilePath(request, out _))
        {
            downloader = null;
            return false;
        }

        if (_registry.TryGetValue(request, out downloader))
            return true;

        downloader = new Downloader(request);
        downloader.DownloadFileCompleted += (_, _) => { _registry.TryRemove(request, out _); };
        return _registry.TryAdd(request, downloader);
    }
}