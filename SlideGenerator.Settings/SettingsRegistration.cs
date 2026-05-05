/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: SettingsRegistration.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

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