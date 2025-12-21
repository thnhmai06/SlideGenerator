using System.Runtime.CompilerServices;
using SlideGenerator.Domain.Configs;

[assembly: InternalsVisibleTo("SlideGenerator.Presentation")]

namespace SlideGenerator.Application.Configs;

public static class ConfigHolder
{
    internal static readonly Lock Locker = new();
    public static Config Value { get; internal set; } = new();

    /// <summary>
    ///     Resets the configuration to its default state by reinitializing the singleton instance.
    /// </summary>
    /// <remarks>
    ///     Call this method to discard any changes made to the current configuration and restore the
    ///     default settings. This method is thread-safe.
    /// </remarks>
    public static void Reset()
    {
        lock (Locker)
        {
            Value = new Config();
        }
    }
}