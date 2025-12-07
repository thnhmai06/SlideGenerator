using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TaoSlideTotNghiep.Config;

/// <summary>
/// Application configuration with singleton pattern and YAML persistence.
/// </summary>
public sealed class AppConfig
{
    #region Constants

    public const string AppName = "tao-slide-tot-nghiep";
    public const string AppType = "data";
    public const string AppDescription = "Data processing application for Tao Slide Tot Nghiep";
    
    private static readonly string DefaultTempPath = Path.Combine(Path.GetTempPath(), AppName, AppType);
    private const string ConfigFileName = "data.config.yaml";

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

    private static readonly Lazy<AppConfig> LazyInstance = new(() => new AppConfig());
    private static readonly object SyncLock = new();

    public static AppConfig Instance => LazyInstance.Value;

    #endregion

    #region Configuration Properties

    public ServerConfig Server { get; set; } = new();
    public DownloadConfig Download { get; set; } = new();
    public SheetConfig Sheet { get; set; } = new();

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
        public string SaveFolder { get; set; } = string.Empty;
        public int MaxWorkers { get; set; } = 5;
        public RetryConfig Retry { get; set; } = new();
        public TimeoutConfig Timeout { get; set; } = new();

        /// <summary>
        /// Gets the effective save folder path.
        /// </summary>
        public string EffectiveSaveFolder => string.IsNullOrEmpty(SaveFolder) ? DefaultTempPath : SaveFolder;
    }

    public class RetryConfig
    {
        public int MaxRetries { get; set; } = 3;
        public double InitialDelay { get; set; } = 1.0;
        public double MaxDelay { get; set; } = 10.0;
        public double Multiplier { get; set; } = 2.0;
        public List<int> OnStatusCodes { get; set; } = [408, 429, 500, 502, 503, 504];
    }

    public class TimeoutConfig
    {
        public int Connect { get; set; } = 10;
        public int Request { get; set; } = 30;
    }

    public class SheetConfig
    {
        public int MaxWorkers { get; set; } = 5;
    }

    #endregion

    #region Core Methods

    private AppConfig()
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

                var loaded = deserializer.Deserialize<AppConfig>(yaml);
                if (loaded != null)
                {
                    Server = loaded.Server ?? Server;
                    Download = loaded.Download ?? Download;
                    Sheet = loaded.Sheet ?? Sheet;
                }
            }
            catch
            {
                // Log error if needed
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
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

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
            Sheet = new SheetConfig();
        }
        Save();
    }

    #endregion
}
