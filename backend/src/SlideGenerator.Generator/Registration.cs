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

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Generator.Abstractions;
using SlideGenerator.Generator.Services;
using SlideGenerator.Image.FaceDetection;
using SlideGenerator.Settings.Config;
using SlideGenerator.Settings.Rules;

namespace SlideGenerator.Generator;

/// <summary>Registers Generator services into the dependency injection container.</summary>
public static class Registration
{
extension(IServiceCollection services)
{
    /// <summary>
    ///     Adds the job runner, its Requests/Jobs persistence, and the <see cref="IService" /> facade to
    ///     the service collection.
    /// </summary>
    /// <returns>The updated service collection.</returns>
    public IServiceCollection AddGeneratorServices()
    {
        services.AddLogging();

        // Named HttpClient for inspect/download calls — proxy is applied on the pooled handler, re-read
        // from ISettingProvider whenever the factory recycles it; per-call timeout is set by callers via
        // Utilities.CreateHttpClientWithSetting (a fresh client is created at the point of use, never shared).
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

        // Shared connection builder for the single Data.db (Recipes/Requests/Jobs tables).
        services.AddSingleton(new SqliteConnectionStringBuilder(NameAndPaths.DataFolder.DataFile.ConnectionString));

        services.AddSingleton<IRequestsRepository, RequestsRepository>();
        services.AddSingleton<IJobsRepository, JobsRepository>();

        // Reads the per-request workflow log file back into scoped LogEntry records for Summary.Logs.
        services.AddSingleton<ILogFileReader, LogFileReader>();

        // Job execution — replaces WorkflowCore entirely.
        services.AddSingleton<IJobRunner, JobRunner>();

        // Service facade — Ipc depends on this, not on IJobRunner directly.
        services.AddSingleton<IService, Service>();

        return services;
    }
}
}
