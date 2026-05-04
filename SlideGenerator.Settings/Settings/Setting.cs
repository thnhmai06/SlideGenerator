namespace SlideGenerator.Settings.Settings;

/// <summary>
///     Represents the root configuration entity containing all application settings.
/// </summary>
public sealed partial class Setting
{
    /// <summary>
    ///     Gets the configuration settings related to image downloading and resource fetching.
    /// </summary>
    public readonly DownloadSetting Download = new();

    /// <summary>
    ///     Gets the configuration settings related to job execution and parallelism.
    /// </summary>
    public readonly JobSetting Job = new();
}