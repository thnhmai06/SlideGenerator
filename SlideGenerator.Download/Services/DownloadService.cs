using Downloader;
using Microsoft.Extensions.Logging;
using SlideGenerator.Settings.Services;

namespace SlideGenerator.Download.Services;

public sealed class DownloadService(ISettingProvider settingProvider, ILogger<DownloadService> logger)
{
    public async Task DownloadAsync(Uri uri, string destinationPath, CancellationToken ct = default)
    {
        var setting = settingProvider.Current;
        var config = new DownloadConfiguration
        {
            BlockTimeout = setting.Download.Retry.Timeout * 1000,
            MaxTryAgainOnFailure = setting.Download.Retry.MaxRetries,
            RequestConfiguration = { Proxy = setting.Download.Proxy.GetWebProxy() }
        };

        logger.LogDebug("Initiating download from {Uri} to {Destination}", uri, destinationPath);

        try
        {
            await using var service = new Downloader.DownloadService(config);
            await service.DownloadFileTaskAsync(uri.ToString(), destinationPath, ct).ConfigureAwait(false);
            logger.LogInformation("Successfully downloaded file to {Destination}", destinationPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download file from {Uri} to {Destination}", uri, destinationPath);
            throw;
        }
    }
}
