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
using SlideGenerator.Generator.Workflows;
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Generator.Workflows.Runners;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Workflows;
using SlideGenerator.Workflows.Engine;
using SlideGenerator.Settings.Immutable;
using SlideGenerator.Settings.Mutable;

namespace SlideGenerator.Generator;

/// <summary>Registers Generator services into the dependency injection container.</summary>
public static class Registration
{
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Adds the workflow runner, its Requests/Workflows persistence, and the <see cref="IService" /> facade to
        ///     the service collection.
        /// </summary>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddGeneratorServices()
        {
            services.AddLogging();
            
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

            // Shared connection builder for the single Data.db (Recipes/Requests/Workflows tables).
            services.AddSingleton(new SqliteConnectionStringBuilder(NameAndPaths.DataFolder.DataFile.ConnectionString));

            services.AddSingleton<IRequestsRepository, RequestsRepository>();
            services.AddSingleton<IWorkflowsRepository, WorkflowsRepository>();

            // Reads the per-request workflow log file back into scoped LogEntry records for Summary.Logs.
            services.AddSingleton<ILogFileReader, LogFileReader>();

            // Workflow execution — generic Workflow Engine (SlideGenerator.Workflows) driving the slide-generation runner.
            services.AddSingleton<IWorkflowConcurrencyProvider, SettingConcurrencyProvider>();
            services.AddSingleton<SlideGenerationRunner>();
            services.AddSingleton<IWorkflowObserver<WorkflowKey, WorkflowSnapshot>, GeneratorWorkflowObserver>();
            services.AddSingleton<IWorkflowResumeSource<WorkflowKey, WorkflowSnapshot>, GeneratorResumeSource>();
            services.AddWorkflowEngine<WorkflowKey, WorkflowSnapshot>();
            services.AddSingleton<IWorkflowController, WorkflowController>();

            // Service facade — Ipc depends on this, not on IWorkflowController directly.
            services.AddSingleton<IService, Service>();

            return services;
        }
    }
}