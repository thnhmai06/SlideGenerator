namespace SlideGenerator.Domain.Settings.Entities;

public sealed partial class Setting
{
    public DownloadSetting Download { get; set; } = new();
    public ImageSetting Image { get; set; } = new();
    public JobSetting Job { get; set; } = new();
}