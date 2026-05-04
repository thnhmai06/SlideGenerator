using System.Net;
using SlideGenerator.Settings.Rules;

namespace SlideGenerator.Settings.Settings;

public sealed partial class Setting
{
    /// <summary>
    ///     Settings governing how the application downloads resources and handles network connectivity.
    /// </summary>
    public sealed class DownloadSetting
    {
        /// <summary>Gets the settings for temporary file storage.</summary>
        public readonly TempSetting Temp = new();
        
        /// <summary>Gets the settings for network proxy configuration.</summary>
        public readonly ProxySetting Proxy = new();
        
        /// <summary>Gets the settings for retry logic and timeouts.</summary>
        public readonly RetrySetting Retry = new();

        /// <summary>
        ///     Defines where temporary files are stored and how directory paths are structured.
        /// </summary>
        public sealed class TempSetting
        {
            /// <summary>
            ///     Gets or sets the base directory for temporary application files.
            ///     Defaults to the system temporary path if not specified.
            /// </summary>
            public string FolderPath
            {
                get => string.IsNullOrEmpty(field) ? NameAndPathRules.DefaultTempPath : field;
                set
                {
                    if (!string.IsNullOrEmpty(value) && !Directory.Exists(value))
                        Directory.CreateDirectory(value);
                    field = value;
                }
            } = string.Empty;

            /// <summary>
            ///     Constructs a specialized directory path for storing raw downloaded images.
            /// </summary>
            /// <param name="bookName">The name of the source workbook.</param>
            /// <param name="sheetName">The name of the source worksheet.</param>
            /// <param name="colName">The name of the column providing the image URI.</param>
            /// <returns>A full directory path for downloads.</returns>
            public string GetDownloadDir(string bookName, string sheetName, string colName)
            {
                bookName = Utilities.NormalizeFileName(bookName);
                sheetName = Utilities.NormalizeFileName(sheetName);
                colName = Utilities.NormalizeFileName(colName);
                return Path.Combine(FolderPath, bookName, sheetName, colName, "Download");
            }

            /// <summary>
            ///     Constructs a specialized directory path for storing edited (cropped/resized) images.
            /// </summary>
            /// <param name="bookName">The name of the source workbook.</param>
            /// <param name="sheetName">The name of the source worksheet.</param>
            /// <param name="colName">The name of the column providing the image URI.</param>
            /// <returns>A full directory path for edited images.</returns>
            public string GetEditDir(string bookName, string sheetName, string colName)
            {
                bookName = Utilities.NormalizeFileName(bookName);
                sheetName = Utilities.NormalizeFileName(sheetName);
                colName = Utilities.NormalizeFileName(colName);
                return Path.Combine(FolderPath, bookName, sheetName, colName, "Edit");
            }
        }

        /// <summary>
        ///     Configures the behavior of network request retries.
        /// </summary>
        public sealed class RetrySetting
        {
            /// <summary>Gets the maximum number of times a failed request should be retried.</summary>
            public int MaxRetries { get; init; } = 3;

            /// <summary>Gets the network timeout in seconds.</summary>
            public int Timeout { get; init; } = 30;
        }

        /// <summary>
        ///     Provides network proxy details for corporate or restricted environments.
        /// </summary>
        public sealed class ProxySetting
        {
            /// <summary>Gets whether a proxy should be used.</summary>
            public bool UseProxy { get; init; } = false;

            /// <summary>Gets the proxy domain name.</summary>
            public string Domain { get; init; } = string.Empty;

            /// <summary>Gets the proxy password.</summary>
            public string Password { get; init; } = string.Empty;

            /// <summary>Gets the full proxy server address (e.g., http://proxy:8080).</summary>
            public string ProxyAddress { get; init; } = string.Empty;

            /// <summary>Gets the proxy username.</summary>
            public string Username { get; init; } = string.Empty;

            /// <summary>
            ///     Constructs an <see cref="IWebProxy"/> based on the current configuration.
            /// </summary>
            /// <returns>A configured web proxy, or null if proxy usage is disabled.</returns>
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