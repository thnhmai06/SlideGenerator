using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Settings.Entities;
using SlideGenerator.Settings.Services;

namespace SlideGenerator.Settings;

/// <summary>
///     Provides extension methods to register settings-related services into the dependency injection container.
/// </summary>
public static class SettingsRegistration
{
    /// <summary>
    ///     Adds settings management, serialization, and provider services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSettingServices(this IServiceCollection services)
    {
        services.AddSingleton<Serializer, YamlSerializer>();

        services.AddSingleton<SettingManager>();
        services.AddSingleton<ISettingProvider>(sp => sp.GetRequiredService<SettingManager>());
        return services;
    }
}