using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Settings.Entities;
using SlideGenerator.Settings.Interfaces;
using SlideGenerator.Settings.Services;

namespace SlideGenerator.Settings;

public static class SettingsRegistration
{
    public static IServiceCollection AddSettingServices(this IServiceCollection services)
    {
        services.AddSingleton<Serializer, YamlSerializer>();
        
        services.AddSingleton<SettingManager>();
        services.AddSingleton<ISettingProvider>(sp => sp.GetRequiredService<SettingManager>());
        return services;
    }
}