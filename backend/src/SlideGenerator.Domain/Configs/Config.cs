namespace SlideGenerator.Domain.Configs;

public sealed partial class Config
{
    public const string FileName = "backend.config.yaml";
    public const string AppName = "SlideGenerator";
    public const string AppDescription = "Backend server of SlideGenerator application.";
    private static readonly string DefaultTempPath = Path.Combine(Path.GetTempPath(), AppName);

    public ServerConfig Server { get; init; } = new();
    public DownloadConfig Download { get; init; } = new();
    public JobConfig Job { get; init; } = new();
    public ImageConfig Image { get; init; } = new();
}