using System.Net;
using Downloader;

namespace SlideGenerator.Domain.Settings.Models;

public sealed partial class Setting
{
    private static readonly string DefaultDownloadPath = Path.Combine(Path.GetTempPath(), AppName);

    public sealed class DownloadSetting
    {
        public bool DeleteAfterDownload = true;
        public int LimitBytesPerSecond = 0;
        public int MaxChunks = 5;

        public ProxySetting Proxy = new();

        public RetrySetting Retry = new();

        public string DownloadFolder
        {
            get => string.IsNullOrEmpty(field) ? DefaultDownloadPath : field;
            set;
        } = string.Empty;

        internal DownloadConfiguration GetConfigurationObject()
        {
            var config = new DownloadConfiguration
            {
                MaximumBytesPerSecond = LimitBytesPerSecond,
                ChunkCount = MaxChunks
            };
            config.ApplyRetrySetting(Retry).ApplyProxySetting(Proxy);
            return config;
        }

        public sealed class RetrySetting
        {
            public int MaxRetries = 3;
            public int Timeout = 30;
        }

        public sealed class ProxySetting
        {
            public string Domain = string.Empty;
            public string Password = string.Empty;
            public string ProxyAddress = string.Empty;
            public bool UseProxy = false;
            public string Username = string.Empty;

            internal IWebProxy? GetWebProxy()
            {
                if (!UseProxy || string.IsNullOrEmpty(ProxyAddress))
                    return null;

                var proxy = new WebProxy(ProxyAddress)
                {
                    Credentials = new NetworkCredential(Username, Password, Domain)
                };
                return proxy;
            }
        }
    }
}

internal static class ApplyConfigurationObjectExtensions
{
    extension(DownloadConfiguration downloadConfiguration)
    {
        internal DownloadConfiguration ApplyRetrySetting(Setting.DownloadSetting.RetrySetting retrySetting)
        {
            downloadConfiguration.MaxTryAgainOnFailure = retrySetting.MaxRetries;
            downloadConfiguration.BlockTimeout = retrySetting.Timeout;
            return downloadConfiguration;
        }

        internal DownloadConfiguration ApplyProxySetting(Setting.DownloadSetting.ProxySetting proxySetting)
        {
            downloadConfiguration.RequestConfiguration.Proxy = proxySetting.GetWebProxy();
            return downloadConfiguration;
        }
    }
}