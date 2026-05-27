/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: Registration.cs
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
using Microsoft.Extensions.Logging;
using SlideGenerator.Cryptography.Application.Abstractions;
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
            sp.GetRequiredService<IEncrypter>(),
            sp.GetRequiredService<ISerializer>(),
            sp.GetService<ILogger<SettingManager>>()));
        services.AddSingleton<ISettingProvider>(sp => sp.GetRequiredService<ISettingManager>());
        return services;
    }
}