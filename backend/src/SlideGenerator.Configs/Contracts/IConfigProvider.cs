using SlideGenerator.Configs.Entities;

namespace SlideGenerator.Configs.Contracts;

/// <summary>
///    Provides read-only access to the current configuration.
/// </summary>
/// <remarks>This interface is intended for components that only need to read configuration values without modifying them.</remarks>
public interface IConfigProvider
{
    /// <summary>
    ///     Gets current configuration.
    /// </summary>
    public Config Current { get; }
}