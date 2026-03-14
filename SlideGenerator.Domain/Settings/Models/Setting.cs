namespace SlideGenerator.Domain.Settings.Models;

public sealed partial class Setting
{
    private const string AppName = "SlideGenerator";

    public DownloadSetting Download = new();
    public ImageSetting Image = new();
    public JobSetting Job = new();
}