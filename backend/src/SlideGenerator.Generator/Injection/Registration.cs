/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
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
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Application.Services;
using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Application.Steps;
using SlideGenerator.Generator.Domain.Models;
using SlideGenerator.Generator.Infrastructure.Middleware;
using SlideGenerator.Generator.Infrastructure.Services;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Infrastructure.Services;
using SlideGenerator.Settings.Application.Abstractions;

namespace SlideGenerator.Generator.Injection;

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
    public static IServiceCollection AddGeneratorServices(this IServiceCollection services)
    {
        services.AddLogging();

        // Concurrency gate limiter — limits resolved at runtime from ISettingProvider
        services.AddSingleton<IGateLocker<GateType>>(sp => new GateLocker<GateType>(
            gate =>
            {
                var settingProvider = sp.GetRequiredService<ISettingProvider>();
                var setting = settingProvider.Current;
                var perf = setting.Performance;
                return gate switch
                {
                    GateType.DownloadImage => perf.MaxParallelDownloadImage,
                    GateType.EditImage => perf.MaxParallelEditImage,
                    GateType.EditPresentation => perf.MaxParallelEditPresentation,
                    GateType.ReadWorkbook => perf.MaxParallelReadWorkbook,
                    GateType.ReadPresentation => perf.MaxParallelReadPresentation,
                    _ => throw new ArgumentOutOfRangeException(nameof(gate), gate, null)
                };
            },
            sp.GetService<ILogger<GateLocker<GateType>>>()));

        // Face-detection pool — limit mirrors EditImage concurrency
        services.AddSingleton<IFaceDetector>(sp =>
        {
            var factory = sp.GetRequiredService<Func<IFaceDetector>>();
            var settings = sp.GetRequiredService<ISettingProvider>();
            return new FaceDetectorPool(factory, () => settings.Current.Performance.MaxParallelEditImage);
        });

        // WorkflowCore Step registrations (Transient — WorkflowCore resolves per-execution via IServiceScope)
        services.AddTransient<LoadRecipeSummary>();
        services.AddTransient<PreflightCleanup>();
        services.AddTransient<ValidateRequest>();
        services.AddTransient<CreateTemplate>();
        services.AddTransient<ExtractData>();
        services.AddTransient<CollectImage>();
        services.AddTransient<EditImage>();
        services.AddTransient<ReplaceSlideData>();
        services.AddTransient<CloseAllHandles>();

        // Step middleware — lazily initializes the workflow logger before each step (supports persistence resume)
        services.AddWorkflowStepMiddleware<GeneratingMiddleware>();

        // Step middleware — publishes StepCompleted progress events with phase info after each step
        services.AddWorkflowStepMiddleware<GeneratingProgressMiddleware>();

        // Workflow service facade — Ipc depends on this, not on WorkflowCore directly
        services.AddSingleton<IGeneratingService, GeneratingService>();

        return services;
    }
}