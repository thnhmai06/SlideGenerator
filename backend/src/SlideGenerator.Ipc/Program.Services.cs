/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Ipc
 * File: Program.Services.cs
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

using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using SlideGenerator.Cloud.Injection;
using SlideGenerator.Coordinator.Injection;
using SlideGenerator.Cryptography.Injection;
using SlideGenerator.Document.Injection;
using SlideGenerator.Generator.Injection;
using SlideGenerator.Image.Injection;
using SlideGenerator.Ipc.Injection;
using SlideGenerator.Logging;
using SlideGenerator.Recipe.Injection;
using SlideGenerator.Settings.Application.Abstractions;
using SlideGenerator.Settings.Domain.Rules;
using SlideGenerator.Settings.Injection;
using SlideGenerator.Summarization.Injection;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SlideGenerator.Ipc;

internal static partial class Program
{
    /// <summary>
    ///     Configures stderr to use UTF-8 so Serilog log output is transmitted correctly
    ///     regardless of the platform's default code page.
    ///     Stdin and stdout are owned by StreamJsonRpc, which writes raw UTF-8 bytes directly.
    /// </summary>
    private static void ConfigureEncoding()
    {
        Console.SetError(new StreamWriter(
            Console.OpenStandardError(),
            new UTF8Encoding(false),
            leaveOpen: true) { AutoFlush = true });
    }

    /// <summary>
    ///     Registers all services into the DI container via per-module extension methods.
    ///     <see cref="StreamJsonRpc.JsonRpc" /> is not registered — it requires stream access available only after host
    ///     construction.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services, ILogger? systemLogger)
    {
        systemLogger?.LogInformation("Registering core services...");
        services.AddTransient(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var level = cfg.GetValue("Logging:Workflow:MinimumLevel", LogEventLevel.Information);
            return new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails();
        });
        services.AddLoggingServices();
        services.AddCryptographyServices();

        systemLogger?.LogInformation("Registering domain modules...");
        services.AddSettingsServices();
        services.AddDocumentServices();
        services.AddCoordinatorServices();
        services.AddCloudServices();
        services.AddImageServices();
        services.AddGeneratorServices();
        services.AddSummarizationServices();
        services.AddRecipeServices();

        systemLogger?.LogInformation("Registering workflow and IPC infrastructure...");
        services.AddWorkflow(x => x.UseSqlite(NameAndPaths.WorkflowsFile.ConnectionString, true));
        services.AddIpcServices();
    }

    /// <summary>Loads persisted settings from the disk before any workflow is started.</summary>
    private static async Task LoadSettingsAsync(IServiceProvider services)
    {
        var settingManager = services.GetRequiredService<ISettingManager>();
        await settingManager.Load().ConfigureAwait(false);
    }

    /// <summary>Persists current settings to disk before the application shutdown.</summary>
    private static async Task SaveSettingsAsync(IServiceProvider services)
    {
        var settingManager = services.GetRequiredService<ISettingManager>();
        await settingManager.Save().ConfigureAwait(false);
    }
}