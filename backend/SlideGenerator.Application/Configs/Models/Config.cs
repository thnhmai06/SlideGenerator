using System.Collections.Immutable;

namespace SlideGenerator.Application.Configs.Models;

public sealed partial class Config
{
    public const string FileName = "backend.config.yaml";
    public const string AppName = "SlideGenerator";
    public const string AppDescription = "Backend server of SlideGenerator application.";
    private static readonly string DefaultTempPath = Path.Combine(Path.GetTempPath(), AppName);

    private static readonly string DefaultOutputPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppName);

    private static readonly string DefaultJobsDatabasePath = Path.Combine(DefaultTempPath, "jobs.db");

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

    public ServerConfig Server { get; init; } = new();
    public DownloadConfig Download { get; init; } = new();
    public JobConfig Job { get; init; } = new();
    public ImageConfig Image { get; init; } = new();
}