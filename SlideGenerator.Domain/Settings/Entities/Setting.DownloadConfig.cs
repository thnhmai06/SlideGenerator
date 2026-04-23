using System.Net;
using SlideGenerator.Domain.Settings.Rules;

namespace SlideGenerator.Domain.Settings.Entities;

public sealed partial class Setting
{
    /// <summary>
    ///     Gets the default temporary download path based on the application name.
    /// </summary>
    private static string DefaultDownloadPath => Path.Combine(Path.GetTempPath(), NamingRules.AppName);

    /// <summary>
    ///     Represents the configuration settings for download operations.
    /// </summary>
    public sealed class DownloadSetting
    {
        /// <summary>
        ///     Gets or sets a value indicating whether downloaded files should be deleted after processing.
        /// </summary>
        public bool DeleteAfterDownload = true;

        /// <summary>
        ///     Gets or sets the maximum download speed in bytes per second. A value of 0 means unlimited.
        /// </summary>
        public int LimitBytesPerSecond = 0;

        /// <summary>
        ///     Gets or sets the maximum number of concurrent chunks used per download.
        /// </summary>
        public int MaxChunks = 5;

        /// <summary>
        ///     Gets or sets the proxy configuration for HTTP requests.
        /// </summary>
        public ProxySetting Proxy = new();

        /// <summary>
        ///     Gets or sets the retry policy configuration for failed downloads.
        /// </summary>
        public RetrySetting Retry = new();

        /// <summary>
        ///     Gets or sets the directory where files will be downloaded. Defaults to <see cref="DefaultDownloadPath" />.
        /// </summary>
        public string DownloadFolder
        {
            get => string.IsNullOrEmpty(field) ? DefaultDownloadPath : field;
            set;
        } = string.Empty;

        /// <summary>
        ///     Represents the retry configuration settings.
        /// </summary>
        public sealed class RetrySetting
        {
            /// <summary>
            ///     Gets or sets the maximum number of retry attempts.
            /// </summary>
            public int MaxRetries = 3;

            /// <summary>
            ///     Gets or sets the timeout duration in seconds for each download attempt.
            /// </summary>
            public int Timeout = 30;
        }

        /// <summary>
        ///     Represents the proxy configuration settings.
        /// </summary>
        public sealed class ProxySetting
        {
            /// <summary>
            ///     Gets or sets the domain for proxy authentication.
            /// </summary>
            public string Domain = string.Empty;

            /// <summary>
            ///     Gets or sets the password for proxy authentication.
            /// </summary>
            public string Password = string.Empty;

            /// <summary>
            ///     Gets or sets the proxy server address.
            /// </summary>
            public string ProxyAddress = string.Empty;

            /// <summary>
            ///     Gets or sets a value indicating whether a proxy server should be used.
            /// </summary>
            public bool UseProxy = false;

            /// <summary>
            ///     Gets or sets the username for proxy authentication.
            /// </summary>
            public string Username = string.Empty;

            /// <summary>
            ///     Creates and returns an <see cref="IWebProxy" /> instance if proxy usage is enabled.
            /// </summary>
            /// <returns>An <see cref="IWebProxy" /> instance if <see cref="UseProxy" /> is <see langword="true" />; otherwise, <see langword="null" />.</returns>
            public IWebProxy? GetWebProxy()
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
