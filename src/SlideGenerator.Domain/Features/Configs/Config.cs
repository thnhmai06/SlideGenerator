namespace SlideGenerator.Domain.Configs;

public sealed partial class Config
{
    public const string FileName = "backend.config.yaml";
    public const string AppName = "SlideGenerator";
    public const string AppDescription = "Backend server of SlideGenerator application.";
    public const string AppUrl = "https://github.com/thnhmai06/SlideGenerator";
    public static readonly string DownloadTempPath = Path.Combine(Path.GetTempPath(), AppName);
    public static readonly string DefaultDatabasePath = Path.Combine(AppContext.BaseDirectory, "Jobs.db");

    public ServerConfig Server { get; init; } = new();
    public DownloadConfig Download { get; init; } = new();
    public JobConfig Job { get; init; } = new();
    public ImageConfig Image { get; init; } = new();
}