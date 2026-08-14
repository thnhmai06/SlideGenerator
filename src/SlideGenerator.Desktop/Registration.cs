/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: Registration.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Desktop.Services.Progress;
using SlideGenerator.Generator.Progress;

namespace SlideGenerator.Desktop;

/// <summary>
///     Provides an extension method to register the in-process progress/log event bus that
///     <c>SlideGenerator.Generator</c>'s <c>Service</c> depends on. ViewModels subscribe to
///     <see cref="GeneratingEventBus" />/<see cref="LogNotifier" /> directly — there is no IPC layer.
/// </summary>
public static class Registration
{
    /// <summary>Adds the Desktop host's event bus implementations to the DI container.</summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDesktopServices(this IServiceCollection services)
    {
        services.AddSingleton<GeneratingEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<GeneratingEventBus>());

        services.AddSingleton<LogNotifier>();
        services.AddSingleton<ILogNotifier>(sp => sp.GetRequiredService<LogNotifier>());

        return services;
    }
}