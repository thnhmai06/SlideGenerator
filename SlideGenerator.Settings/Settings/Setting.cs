namespace SlideGenerator.Settings.Settings;

/// <summary>
///     Represents the root configuration entity containing all application settings.
/// </summary>
public sealed partial class Setting
{
    public readonly DownloadSetting Download = new();
    public readonly JobSetting Job = new();
}