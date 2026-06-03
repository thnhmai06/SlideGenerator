/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: Registration.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlideGenerator.Settings.Application.Abstractions;
using SlideGenerator.Settings.Infrastructure.Services;

namespace SlideGenerator.Settings.Injection;

/// <summary>
///     Provides extension methods to register settings-related services into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Adds settings management, serialization, and provider services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSettingsServices(this IServiceCollection services)
    {
        services.AddSingleton<ISerializer, YamlSerializer>();

        services.AddSingleton<ISettingManager>(sp => new SettingManager(
            sp.GetRequiredService<ISerializer>(),
            sp.GetService<ILogger<SettingManager>>()));
        services.AddSingleton<ISettingProvider>(sp => sp.GetRequiredService<ISettingManager>());
        services.AddTransient<ISettingCalibrator, SettingCalibrator>();
        return services;
    }
}
