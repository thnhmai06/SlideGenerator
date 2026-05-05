namespace SlideGenerator.Settings.Models;

/// <summary>
///     Represents the root configuration entity containing all application settings.
/// </summary>
public sealed partial record Setting
{
    /// <summary>
    ///     Gets the configuration settings related to image downloading and resource fetching.
    /// </summary>
    public DownloadSetting Download { get; init; } = new();

    /// <summary>
    ///     Gets the configuration settings related to job execution and parallelism.
    /// </summary>
    public JobSetting Job { get; init; } = new();
}