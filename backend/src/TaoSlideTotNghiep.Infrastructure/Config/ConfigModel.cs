using System.Collections.Immutable;

namespace TaoSlideTotNghiep.Infrastructure.Config;

/// <summary>
/// Server configuration.
/// </summary>
public sealed class ConfigModel
{

    #region Statics

    public const string AppName = "tao-slide-tot-nghiep";
    public const string AppDescription = "Backend server for Tao Slide Tot Nghiep";
    private static readonly string DefaultTempPath = Path.Combine(Path.GetTempPath(), AppName);
    public static readonly IImmutableSet<string> ImageExtensions =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase,
            // Common
            "jpg", "jpeg", "png", "gif", "webp", "bmp", "ico",
            // HEIF/AVIF
            "heic", "heif", "avif",
            // TIFF and others
            "tif", "tiff", "psd", "tga",
            // Additional formats
            "cur", "dcx", "dds", "icns", "pcx", "ppm", "pgm", "pbm", "pnm",
            "sgi", "xbm", "xpm", "rgb", "rgba"
        );
    public static readonly IImmutableSet<string> SpreadsheetExtensions =
        ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase,
            "xlsx", "xlsm", "xltx", "xltm"
        );

    #endregion

    #region Properties

    public ServerConfig Server { get; internal set; } = new();
    public DownloadConfig Download { get; internal set; } = new();

    #endregion

    #region Nested Config

    public sealed class ServerConfig
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5000;
        public bool Debug { get; set; } = false;
    }

    public sealed class DownloadConfig
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
}