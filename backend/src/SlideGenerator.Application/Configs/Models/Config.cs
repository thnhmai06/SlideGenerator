using System.Collections.Immutable;

namespace SlideGenerator.Application.Configs.Models;

public sealed partial class Config
{
    public const string FileName = "backend.config.yaml";
    public const string AppName = "tao-slide-tot-nghiep";
    public const string AppDescription = "Backend server for Tao Slide Tot Nghiep";
    private static readonly string DefaultTempPath = Path.Combine(Path.GetTempPath(), AppName);

    private static readonly string DefaultOutputPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppName);

    private static readonly string DefaultHangfireDbPath = Path.Combine(DefaultTempPath, "hangfire.db");

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

    public static readonly IImmutableSet<char> InvalidPathChars =
        ImmutableHashSet.Create(Path.GetInvalidFileNameChars());

    public ServerConfig Server { get; init; } = new();
    public DownloadConfig Download { get; init; } = new();
    public JobConfig Job { get; init; } = new();
}