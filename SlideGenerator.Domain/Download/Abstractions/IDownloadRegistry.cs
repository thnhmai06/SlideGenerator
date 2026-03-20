using System.Diagnostics.CodeAnalysis;
using SlideGenerator.Domain.Download.Entities;
using SlideGenerator.Domain.Download.Models;

namespace SlideGenerator.Domain.Download.Abstractions;

public interface IDownloadRegistry
{
    IReadOnlyDictionary<DownloadRequest, IDownloader> RegistryView { get; }

    bool TryGetCompletedDownloadFilePath(DownloadRequest request,
        [MaybeNullWhen(false)] out string filePath);

    bool TryGetOrCreateDownloader(DownloadRequest request, DownloadConfiguration? config,
        [MaybeNullWhen(false)] out IDownloader downloader);
}