using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using SlideGenerator.Application.Download.Entities;
using SlideGenerator.Domain.Download.Abstractions;
using SlideGenerator.Domain.Download.Entities;
using SlideGenerator.Domain.Download.Models;
using SlideGenerator.Domain.Download.Rules;

namespace SlideGenerator.Application.Download.Services;

public sealed class DownloadRegistry : IDownloadRegistry
{
    private readonly ConcurrentDictionary<DownloadRequest, IDownloader> _registry = new();
    public IReadOnlyDictionary<DownloadRequest, IDownloader> RegistryView => _registry;

    public bool TryGetCompletedDownloadFilePath(DownloadRequest request,
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

    public bool TryGetOrCreateDownloader(DownloadRequest request, DownloadConfiguration? config,
        [MaybeNullWhen(false)] out IDownloader downloader)
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