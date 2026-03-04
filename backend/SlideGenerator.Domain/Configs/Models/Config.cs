namespace SlideGenerator.Domain.Configs.Models;

public sealed partial class Config
{
    private const string AppName = "SlideGenerator";

    public DownloadConfig Download = new();
    public ImageConfig Image = new();
    public JobConfig Job = new();
}