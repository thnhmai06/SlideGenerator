using SlideGenerator.Settings.Models;

namespace SlideGenerator.Settings.Services;

/// <summary>
///     Provides read-only access to the current application configuration.
/// </summary>
public interface ISettingProvider
{
    /// <summary>
    ///     Gets the current active <see cref="Setting" /> configuration.
    /// </summary>
    public Setting Current { get; }
}