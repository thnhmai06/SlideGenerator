/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
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
using SlideGenerator.Generating.Application.Steps;
using SlideGenerator.Generating.Application.Workflows;
using SlideGenerator.Generating.Domain.Models.Contexts;
using SlideGenerator.Generating.Infrastructure.Middleware;
using SlideGenerator.Generating.Infrastructure.Services;
using SlideGenerator.Settings.Domain.Rules;
using WorkflowCore.Interface;

namespace SlideGenerator.Generating.Injection;

/// <summary>
///     Provides extension methods to register the generating workflow and its steps
///     into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Adds the generating workflow, all associated WorkflowCore steps, and the
    ///     <see cref="IGeneratingService" /> facade to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddGeneratingServices(this IServiceCollection services)
    {
        // WorkflowCore Step registrations (Transient — WorkflowCore resolves per-execution)
        services.AddTransient<ValidateRequest>();
        services.AddTransient<CreateTemplate>();
        services.AddTransient<ExtractData>();
        services.AddTransient<DownloadImage>();
        services.AddTransient<EditImage>();
        services.AddTransient<ReplaceSlideData>();
        services.AddTransient<CloseAllHandles>();

        // Workflow definition (WorkflowCore resolves via IWorkflowHost.RegisterWorkflow<>)
        services.AddTransient<IWorkflow<GeneratingContext>, GeneratingWorkflow>();

        // Step middleware — lazily initializes the workflow logger before each step (supports persistence resume)
        services.AddWorkflowStepMiddleware<GeneratingMiddleware>();

        // Step middleware — publishes StepCompleted progress events with phase info after each step
        services.AddWorkflowStepMiddleware<GeneratingProgressMiddleware>();

        // Workflow service facade — Ipc depends on this, not on WorkflowCore directly
        services.AddSingleton<IGeneratingService, GeneratingService>();

        // Recipe repository — deduplicates by content, caches results in memory
        services.AddSingleton<IRecipeRepository>(_ => new RecipeRepository(NameAndPaths.RecipesFile.ConnectionString));

        return services;
    }
}