using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Domain.Config;

/// <summary>
/// Application configuration with singleton pattern and YAML persistence.
/// </summary>
public sealed class BackendConfig
{
    #region Constants

    public static readonly string AppName = "tao-slide-tot-nghiep";
    public static readonly string AppDescription = "Backend server for Tao Slide Tot Nghiep";
    private static readonly string DefaultTempPath = Path.Combine(Path.GetTempPath(), AppName);
    private static readonly string ConfigFileName = "backend.config.yaml";

    /// <summary>
    /// Supported image file extensions.
    /// </summary>
    public static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common
        "jpg", "jpeg", "png", "gif", "webp", "bmp", "ico",
        // HEIF/AVIF
        "heic", "heif", "avif",
        // TIFF and others
        "tif", "tiff", "psd", "tga",
        // Additional formats
        "cur", "dcx", "dds", "icns", "pcx", "ppm", "pgm", "pbm", "pnm",
        "sgi", "xbm", "xpm", "rgb", "rgba"
        // and more... but not included
    };

    /// <summary>
    /// Supported spreadsheet file extensions.
    /// </summary>
    public static readonly HashSet<string> SpreadsheetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "xlsx", "xlsm", "xltx", "xltm"
    };

    #endregion

    #region Singleton

    private static readonly Lazy<BackendConfig> LazyInstance = new(() => new BackendConfig());
    private static readonly Lock SyncLock = new();

    public static BackendConfig Instance => LazyInstance.Value;

    #endregion

    #region Configuration Properties

    public ServerConfig Server { get; private set; } = new();
    public DownloadConfig Download { get; private set; } = new();

    #endregion

    #region Nested Config Classes

    public class ServerConfig
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5000;
        public bool Debug { get; set; } = false;
    }

    public class DownloadConfig
    {
        public class RetryConfig
        {
            public int Timeout { get; set; } = 30;
            public int MaxRetries { get; set; } = 3;
        }

        public int MaxChunks { get; set; } = 5;
        public int LimitBytesPerSecond { get; set; } = 0;

        public string SaveFolder
        {
            get => string.IsNullOrEmpty(field) ? DefaultTempPath : field;
            set;
        } = string.Empty;

        public RetryConfig Retry { get; set; } = new();
    }

    #endregion

    #region Core Methods

    private BackendConfig()
    {
        Reload();
    }

    /// <summary>
    /// Reloads configuration from the YAML file.
    /// </summary>
    public void Reload()
    {
        lock (SyncLock)
        {
            if (!File.Exists(ConfigFileName))
            {
                Save();
                return;
            }

            try
            {
                var yaml = File.ReadAllText(ConfigFileName);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var loaded = deserializer.Deserialize<BackendConfig>(yaml);
                Server = loaded.Server;
                Download = loaded.Download;
            }
            catch
            {
                ResetToDefaults();
            }
        }
    }

    /// <summary>
    /// Saves current configuration to the YAML file.
    /// </summary>
    public void Save()
    {
        lock (SyncLock)
        {
            var directory = Path.GetDirectoryName(ConfigFileName);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(this);
            File.WriteAllText(ConfigFileName, yaml);
        }
    }

    /// <summary>
    /// Resets configuration to default values.
    /// </summary>
    public void ResetToDefaults()
    {
        lock (SyncLock)
        {
            Server = new ServerConfig();
            Download = new DownloadConfig();
        }

        Save();
    }

    #endregion
}