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
using SlideGenerator.Settings.Mutable;

namespace SlideGenerator.Settings;

/// <summary>
///     Provides extension methods to register settings-related services into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Adds settings management and provider services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSettingsServices(this IServiceCollection services)
    {
        services.AddSingleton<ISettingManager>(sp => new SettingManager(
            sp.GetService<ILogger<SettingManager>>()));
        services.AddSingleton<ISettingProvider>(sp => sp.GetRequiredService<ISettingManager>());
        return services;
    }
}