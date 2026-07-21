/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: Registration.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Stdio.Handlers;
using SlideGenerator.Stdio.Implementations;

namespace SlideGenerator.Stdio;

/// <summary>
///     Provides extension methods to register all IPC infrastructure and handler services
///     into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Adds all IPC services: the workflow event bus, progress observer,
    ///     and all JSON-RPC method handlers.
    ///     The <see cref="StreamJsonRpc.JsonRpc" /> connection is created in <c>Program.cs</c>
    ///     after the host is built, and is not registered in the container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddIpcServices(this IServiceCollection services)
    {
        // Progress event bus — registered as both interface (for Generator) and concrete (for the coalescer)
        services.AddSingleton<GeneratingEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<GeneratingEventBus>());

        // Log notification bus — same reasoning as GeneratingEventBus, for the Logging → Middleware → coalescer path
        services.AddSingleton<LogNotifier>();
        services.AddSingleton<ILogNotifier>(sp => sp.GetRequiredService<LogNotifier>());

        services.AddSingleton<ProgressCoalescer>();

        // JSON-RPC method handlers
        services.AddSingleton<GeneratingActiveHandler>();
        services.AddSingleton<GeneratingCompletedHandler>();
        services.AddSingleton<RecipeHandler>();
        services.AddSingleton<SummarizationHandler>();
        services.AddSingleton<SettingsHandler>();

        return services;
    }
}