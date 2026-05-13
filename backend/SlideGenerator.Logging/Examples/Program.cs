/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: Program.cs
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
using Microsoft.Extensions.Hosting;
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Logging.Infrastructure.Services;

namespace SlideGenerator.Logging.Examples;

/// <summary>
///     Demonstrates how an application composes the logging module at the edge.
/// </summary>
public static class Program
{
    /// <summary>
    ///     Example entry point for a host application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>A completed task.</returns>
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var systemLogger = SystemLoggerBootstrapper.Initialize("logs/system", builder.Configuration);
        builder.Services.AddLoggingModule(builder.Configuration);
        builder.Services.AddSystemLogging(systemLogger);
        builder.Services.AddTransient<AuthService>();

        using var host = builder.Build();
        await host.StartAsync().ConfigureAwait(false);

        using (systemLogger.BeginScope("System/Startup"))
        {
            systemLogger.Information("Application started.");
        }

        var authService = host.Services.GetRequiredService<AuthService>();
        authService.Login("demo-user");

        await host.StopAsync().ConfigureAwait(false);
    }
}

internal sealed class AuthService(IAppLoggerFactory loggerFactory)
{
    private readonly IAppLogger _logger = loggerFactory.CreateLogger("Auth", "logs/Auth.log");

    public void Login(string userId)
    {
        using (_logger.BeginScope("Auth/Login"))
        {
            _logger.Information("Login requested for {UserId}", userId);

            try
            {
                ThrowInvalidCredentials(userId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Login failed for {UserId}", userId);
            }
        }
    }

    private static void ThrowInvalidCredentials(string userId)
    {
        throw new InvalidOperationException($"Invalid credentials for {userId}.");
    }
}



