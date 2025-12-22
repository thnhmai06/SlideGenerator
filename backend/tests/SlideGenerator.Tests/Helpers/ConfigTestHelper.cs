using System.Reflection;
using SlideGenerator.Application.Configs;
using SlideGenerator.Domain.Configs;

namespace SlideGenerator.Tests.Helpers;

internal static class ConfigTestHelper
{
    public static Config GetConfig()
    {
        return ConfigHolder.Value;
    }

    public static void SetConfig(Config config)
    {
        var property = typeof(ConfigHolder).GetProperty("Value",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (property == null)
            throw new InvalidOperationException("ConfigHolder.Value property not found.");
        property.SetValue(null, config);
    }
}