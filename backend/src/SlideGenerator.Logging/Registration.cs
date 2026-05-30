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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SlideGenerator.Logging.Abstractions;
using SlideGenerator.Logging.Services;

namespace SlideGenerator.Logging;

/// <summary>
///     Registers logging services.
/// </summary>
public static class Registration
{
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Adds the file logger factory service.
        /// </summary>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddLoggingServices()
        {
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });
            services
                .AddTransient<IFileLoggerFactory,
                    SerilogFileLoggerFactory>(); // must have to be Transient, not Singleton
            return services;
        }
    }
}