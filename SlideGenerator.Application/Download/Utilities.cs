using SlideGenerator.Application.Download.Models;
using SlideGenerator.Domain.Settings.Entities;

namespace SlideGenerator.Application.Download;

public static class Utilities
{
    public static DownloadConfiguration GetConfigurationObject(this Setting.DownloadSetting downloadSetting)
    {
        var config = new DownloadConfiguration
        {
            MaximumBytesPerSecond = downloadSetting.LimitBytesPerSecond,
            ChunkCount = downloadSetting.MaxChunks
        };
        config.ApplyRetrySetting(downloadSetting.Retry).ApplyProxySetting(downloadSetting.Proxy);
        return config;
    }
    
    extension(DownloadConfiguration downloadConfiguration)
    {
        private DownloadConfiguration ApplyRetrySetting(Setting.DownloadSetting.RetrySetting retrySetting)
        {
            downloadConfiguration.MaxTryAgainOnFailure = retrySetting.MaxRetries;
            downloadConfiguration.BlockTimeout = retrySetting.Timeout;
            return downloadConfiguration;
        }

        private DownloadConfiguration ApplyProxySetting(Setting.DownloadSetting.ProxySetting proxySetting)
        {
            downloadConfiguration.Proxy = proxySetting.GetWebProxy();
            return downloadConfiguration;
        }
    }
}