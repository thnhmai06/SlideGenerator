/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Ipc
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
using SlideGenerator.Generating.Application.Abstractions;
using SlideGenerator.Ipc.Handlers;
using SlideGenerator.Ipc.Infrastructure;

namespace SlideGenerator.Ipc;

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
        // Workflow progress event bus — registered as both interface (for Pipeline) and concrete (for Observer)
        services.AddSingleton<GeneratingEventBus>();
        services.AddSingleton<IGeneratingEventBus>(sp => sp.GetRequiredService<GeneratingEventBus>());
        services.AddSingleton<WorkflowProgressObserver>();

        // JSON-RPC method handlers
        services.AddSingleton<GeneratingActiveHandler>();
        services.AddSingleton<GeneratingCompletedHandler>();
        services.AddSingleton<GeneratingRecipeHandler>();
        services.AddSingleton<ScanningHandler>();
        services.AddSingleton<SettingsHandler>();

        return services;
    }
}