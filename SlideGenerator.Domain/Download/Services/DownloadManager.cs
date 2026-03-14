using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Downloader;
using SlideGenerator.Domain.Download.Entities;

namespace SlideGenerator.Domain.Download.Services;

public class DownloadManager
{
    private readonly ConcurrentDictionary<FileDownloadInfo, FileDownloader> _currentDownload = new();

    public static bool IsDownloadCompleted(FileDownloadInfo info, [MaybeNullWhen(false)] out string filePath)
    {
        var files = Directory.GetFiles(info.SaveFolder, $"{info.FileName}.*");
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file);
            if (ext.Equals(FileDownloader.TempExtension, StringComparison.OrdinalIgnoreCase)) continue;
            filePath = file;
            return true;
        }

        filePath = null;
        return false;
    }

    public bool TryGetOrAddDownloader(FileDownloadInfo info, DownloadConfiguration config,
        [MaybeNullWhen(false)] out FileDownloader downloader)
    {
        if (IsDownloadCompleted(info, out _))
        {
            downloader = null;
            return false;
        }

        if (_currentDownload.TryGetValue(info, out downloader))
            return true;

        downloader = new FileDownloader(info, config);
        downloader.Service.DownloadFileCompleted += (_, _) => { _currentDownload.TryRemove(info, out _); };
        return _currentDownload.TryAdd(info, downloader);
    }
}