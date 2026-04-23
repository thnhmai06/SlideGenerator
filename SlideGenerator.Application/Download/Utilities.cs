using SlideGenerator.Application.Download.Models;
using SlideGenerator.Domain.Settings.Entities;

namespace SlideGenerator.Application.Download;

/// <summary>
///     Provides extension methods and utilities for downloading operations.
/// </summary>
public static class Utilities
{
    /// <summary>
    ///     Converts a <see cref="Setting.DownloadSetting" /> into a concrete <see cref="DownloadConfiguration" /> object.
    /// </summary>
    /// <param name="downloadSetting">The raw download settings entity.</param>
    /// <returns>A fully configured <see cref="DownloadConfiguration" /> ready for use by download services.</returns>
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
    
    /// <summary>
    ///     Internal extension methods to apply specific setting groups to a <see cref="DownloadConfiguration" />.
    /// </summary>
    extension(DownloadConfiguration downloadConfiguration)
    {
        /// <summary>
        ///     Applies retry settings to the configuration.
        /// </summary>
        /// <param name="retrySetting">The retry settings to apply.</param>
        /// <returns>The updated <see cref="DownloadConfiguration" /> instance.</returns>
        private DownloadConfiguration ApplyRetrySetting(Setting.DownloadSetting.RetrySetting retrySetting)
        {
            downloadConfiguration.MaxTryAgainOnFailure = retrySetting.MaxRetries;
            downloadConfiguration.BlockTimeout = retrySetting.Timeout;
            return downloadConfiguration;
        }

        /// <summary>
        ///     Applies proxy settings to the configuration.
        /// </summary>
        /// <param name="proxySetting">The proxy settings to apply.</param>
        /// <returns>The updated <see cref="DownloadConfiguration" /> instance.</returns>
        private DownloadConfiguration ApplyProxySetting(Setting.DownloadSetting.ProxySetting proxySetting)
        {
            downloadConfiguration.Proxy = proxySetting.GetWebProxy();
            return downloadConfiguration;
        }
    }
}
