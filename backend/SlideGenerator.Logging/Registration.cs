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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Logging.Infrastructure.Options;
using SlideGenerator.Logging.Infrastructure.Services;

namespace SlideGenerator.Logging;

/// <summary>
///     Registers logging services.
/// </summary>
public static class Registration
{
    /// <param name="services">The service collection to update.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Adds the logging abstraction and normal logger factory.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddLoggingModule(IConfiguration configuration)
        {
            services.AddSingleton(LoggingOptionsReader.Read(configuration));
            services.AddSingleton<IScopeManager, SerilogScopeManager>();
            services.AddSingleton<IAppLoggerFactory, SerilogAppLoggerFactory>();
            return services;
        }

        /// <summary>
        ///     Adds the logging abstraction and normal logger factory with default settings.
        /// </summary>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddLoggingModule()
        {
            services.AddSingleton(new LoggingOptions());
            services.AddSingleton<IScopeManager, SerilogScopeManager>();
            services.AddSingleton<IAppLoggerFactory, SerilogAppLoggerFactory>();
            return services;
        }

        /// <summary>
        ///     Adds an already initialized System logger to the service collection.
        /// </summary>
        /// <param name="systemLogger">The System logger.</param>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddSystemLogging(ISystemLogger systemLogger)
        {
            services.AddSingleton(systemLogger);
            services.AddSingleton<IAppLogger>(systemLogger);
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(Log.Logger, false);
            });

            return services;
        }
    }
}



