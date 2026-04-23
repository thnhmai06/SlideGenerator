namespace SlideGenerator.Domain.Settings.Entities;

/// <summary>
///     Represents the root configuration entity containing all application settings.
/// </summary>
public sealed partial class Setting
{
    /// <summary>
    ///     Gets or sets the download configuration settings.
    /// </summary>
    public DownloadSetting Download { get; set; } = new();

    /// <summary>
    ///     Gets or sets the image processing configuration settings.
    /// </summary>
    public ImageSetting Image { get; set; } = new();

    /// <summary>
    ///     Gets or sets the job execution configuration settings.
    /// </summary>
    public JobSetting Job { get; set; } = new();
}
