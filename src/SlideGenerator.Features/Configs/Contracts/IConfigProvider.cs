using SlideGenerator.Features.Configs.Entities;

namespace SlideGenerator.Features.Configs.Contracts;

/// <summary>
///     Provides read-only access to the current configuration.
/// </summary>
/// <remarks>
/// Reviewed by @thnhmai06 at 01/03/2026 00:36:50 GMT+7
/// This interface is intended for components that only need to read configuration values without modifying them.
/// </remarks>
public interface IConfigProvider
{
    /// <summary>
    ///     Gets current configuration.
    /// </summary>
    public Config Current { get; }
}