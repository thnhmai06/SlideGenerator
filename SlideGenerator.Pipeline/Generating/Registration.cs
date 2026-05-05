/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Pipeline
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
using SlideGenerator.Pipeline.Generating.Steps;
using SlideGenerator.Pipeline.Generating.Workflows;
using SlideGenerator.Pipeline.Generating.Workflows.Models;
using WorkflowCore.Interface;

namespace SlideGenerator.Pipeline.Generating;

/// <summary>
///     Provides extension methods to register the generating workflow and its steps
///     into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Adds the generating workflow and all associated steps to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddGeneratingServices(this IServiceCollection services)
    {
        // Step Registrations
        services.AddTransient<ValidateRequest>();
        services.AddTransient<CreateTemplate>();
        services.AddTransient<ExtractData>();
        services.AddTransient<DownloadImage>();
        services.AddTransient<EditImage>();
        services.AddTransient<ReplaceSlideData>();
        services.AddTransient<CloseAllHandles>();

        // Workflow Registration
        // Note: WorkflowCore expects the workflow to be registered via IWorkflowHost,
        // but often we just register the IWorkflow implementation in DI.
        services.AddTransient<IWorkflow<GeneratingTask>, GeneratingWorkflow>();

        return services;
    }
}