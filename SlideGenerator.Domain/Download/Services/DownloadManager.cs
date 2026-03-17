using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Downloader;
using SlideGenerator.Domain.Download.Entities;

namespace SlideGenerator.Domain.Download.Services;

public class DownloadManager
{
    private readonly ConcurrentDictionary<DownloadInfo, Entities.Downloader> _currentDownload = new();

    public static bool IsDownloadCompleted(DownloadInfo info, [MaybeNullWhen(false)] out string filePath)
    {
        var files = Directory.GetFiles(info.SaveFolder, $"{info.FileName}.*");
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file);
            if (ext.Equals(Entities.Downloader.TempExtension, StringComparison.OrdinalIgnoreCase)) continue;
            filePath = file;
            return true;
        }

        filePath = null;
        return false;
    }

    public bool TryGetOrCreateDownloader(DownloadInfo info, DownloadConfiguration? config,
        [MaybeNullWhen(false)] out Entities.Downloader downloader)
    {
        if (IsDownloadCompleted(info, out _))
        {
            downloader = null;
            return false;
        }

        if (_currentDownload.TryGetValue(info, out downloader))
            return true;

        downloader = new Entities.Downloader(info, config ?? new DownloadConfiguration());
        downloader.Service.DownloadFileCompleted += (_, _) => { _currentDownload.TryRemove(info, out _); };
        return _currentDownload.TryAdd(info, downloader);
    }
}