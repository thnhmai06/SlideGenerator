/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Logging.Infrastructure.Options;
using SlideGenerator.Logging.Infrastructure.Services;

namespace SlideGenerator.Logging.Injection;

/// <summary>
///     Registers logging services.
/// </summary>
public static class Registration
{
    /// <param name="services">The service collection to update.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Adds the logging factory and bridges Serilog as the MEL provider.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddLoggingServices(IConfiguration? configuration = null)
        {
            services.AddSingleton(configuration is null
                ? new LoggingOptions()
                : LoggingOptionsReader.Read(configuration));
            services.AddSingleton<IAppLoggerFactory, SerilogAppLoggerFactory>();
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(Log.Logger);
            });
            return services;
        }
    }
}