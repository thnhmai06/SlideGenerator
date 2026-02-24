using Downloader;
using SlideGenerator.Configs.Contracts;
using SlideGenerator.Configs.Entities;

namespace SlideGenerator.Generating.Services;

/// <summary>
///     Remote file downloader.
/// </summary>
public sealed class DownloadService
{
    private readonly Config _config;

    /// <summary>
    ///     Initializes download service with config manager.
    /// </summary>
    /// <param name="configProvider">Read-only configuration manager.</param>
    public DownloadService(IConfigProvider configProvider)
    {
        _config = configProvider.Current;
    }

    /// <summary>
    ///     Downloads a remote resource and returns bytes.
    /// </summary>
    /// <param name="uri">Remote URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<byte[]> DownloadAsync(Uri uri, CancellationToken cancellationToken)
    {
        var saveFolder = Path.GetFullPath(_config.Download.SaveFolder);
        Directory.CreateDirectory(saveFolder);

        var fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = $"download_{Guid.NewGuid():N}.bin";

        var destinationPath = Path.Combine(saveFolder, $"{Guid.NewGuid():N}_{fileName}");

        var configuration = new DownloadConfiguration
        {
            ChunkCount = Math.Max(1, _config.Download.MaxChunks),
            MaximumBytesPerSecond = Math.Max(0, _config.Download.LimitBytesPerSecond),
            MaxTryAgainOnFailure = Math.Max(0, _config.Download.Retry.MaxRetries),
            Timeout = Math.Max(1, _config.Download.Retry.Timeout * 1000),
            RequestConfiguration =
            {
                Proxy = _config.Download.Proxy.GetWebProxy()
            }
        };

        var downloader = new Downloader.DownloadService(configuration);
        await downloader.DownloadFileTaskAsync(uri.ToString(), destinationPath, cancellationToken).ConfigureAwait(false);

        try
        {
            return await File.ReadAllBytesAsync(destinationPath, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (_config.Download.DeleteAfterDownload && File.Exists(destinationPath))
                File.Delete(destinationPath);
        }
    }
}
