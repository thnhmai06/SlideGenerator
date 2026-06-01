/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
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
                builder.AddSerilog(dispose: false);
            });
            services
                .AddTransient<IFileLoggerFactory,
                    SerilogFileLoggerFactory>(); // must have to be Transient, not Singleton
            return services;
        }
    }
}