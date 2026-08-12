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
using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Application.Steps;
using SlideGenerator.Generator.Infrastructure.Middleware;
using SlideGenerator.Generator.Infrastructure.Services;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Infrastructure.Services;
using SlideGenerator.Settings.Abstractions;
using SlideGenerator.Settings.Rules;

namespace SlideGenerator.Generator.Injection;

/// <summary>Registers Generator services into the dependency injection container.</summary>
public static class Registration
{
extension(IServiceCollection services)
{
    /// <summary>
    ///     Adds the generating workflow, all associated WorkflowCore steps, and the
    ///     <see cref="IService" /> facade to the service collection.
    /// </summary>
    /// <returns>The updated service collection.</returns>
    public IServiceCollection AddGeneratorServices()
    {
        services.AddLogging();

        // Named HttpClient for inspect/download calls — proxy is applied on the pooled handler, re-read
        // from ISettingProvider whenever the factory recycles it; per-call timeout is set by callers via
        // Utilities.CreateHttpClientWithSetting (steps create a fresh client at the point of use, never share one).
        services.AddHttpClient(NameAndPaths.Application.Name)
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var proxy = sp.GetRequiredService<ISettingProvider>().Current.Network.Proxy;
                return new HttpClientHandler
                {
                    UseProxy = proxy.UseProxy,
                    Proxy = proxy.GetWebProxy()
                };
            });

        // Face-detection pool — bounded by logical CPU count (CPU-bound native OpenCV work)
        services.AddSingleton<IFaceDetector>(sp =>
        {
            var factory = sp.GetRequiredService<Func<IFaceDetector>>();
            return new FaceDetectorPool(factory, () => (uint)Environment.ProcessorCount);
        });

        // Shared, app-wide cache for resolved URLs and downloaded files — reads its connection string
        // from Settings directly (no DI'd SqliteConnectionStringBuilder, avoids colliding with the one
        // Recipe module registers for Recipes.db).
        services.AddSingleton<ICache, SqliteCache>();

        // Progress persistence (Requests/Jobs/Rows) — same reasoning, own connection string, no collision
        // with Cache.db/Recipes.db.
        services.AddSingleton<IStudioRepository, StudioRepository>();

        // Reads the per-request workflow log file back into scoped LogEntry records for Summary.Logs.
        services.AddSingleton<ILogFileReader, LogFileReader>();

        // WorkflowCore Step registrations (Transient — WorkflowCore resolves per-execution via IServiceScope)
        services.AddTransient<PreflightCleanup>();
        services.AddTransient<InspectUrlsStep>();
        services.AddTransient<GenerateJobStep>();

        // Step middleware — lazily initializes the workflow logger before each step (supports persistence resume)
        services.AddWorkflowStepMiddleware<Middleware>();

        // Workflow service facade — Ipc depends on this, not on WorkflowCore directly
        services.AddSingleton<IService, Service>();

        return services;
    }
}
}
