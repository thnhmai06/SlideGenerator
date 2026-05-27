/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
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
using SlideGenerator.Cloud.Application.Abstractions;
using SlideGenerator.Cloud.Infrastructure.Services;

namespace SlideGenerator.Cloud.Injection;

/// <summary>
///     Registers collector services into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <param name="services">The service collection to update.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Adds cloud resolver, HTTP client factory, and file collector services.
        /// </summary>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddCloudServices()
        {
            services.AddSingleton<ICloudClient>(sp =>
                new CloudClient(sp.GetService<ILogger<CloudClient>>()));
            services.AddSingleton<ICloudResolver>(sp =>
                new CloudResolver(
                    sp.GetRequiredService<ICloudClient>(),
                    sp.GetService<ILogger<CloudResolver>>()));
            return services;
        }
    }
}