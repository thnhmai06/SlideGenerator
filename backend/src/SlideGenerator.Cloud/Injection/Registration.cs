/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
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