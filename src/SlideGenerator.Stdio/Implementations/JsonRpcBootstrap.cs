/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: JsonRpcBootstrap.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Stdio.Handlers;
using SlideGenerator.Stdio.Implementations.Adapters;
using StreamJsonRpc;

namespace SlideGenerator.Stdio.Implementations;

/// <summary>
///     Configures and wires the <see cref="JsonRpc" /> connection for the IPC sidecar.
/// </summary>
internal static class JsonRpcBootstrap
{
    /// <summary>
    ///     Builds the shared <see cref="JsonSerializerOptions" /> used by the <see cref="SystemTextJsonFormatter" />.
    /// </summary>
    internal static JsonSerializerOptions BuildJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new Vector2JsonConverter(),
                new RoiOptionJsonAdapter(),
                new RectangleFJsonAdapter(),
                new JsonStringEnumConverter()
            }
        };
    }

    /// <summary>
    ///     Creates and starts the <see cref="JsonRpc" /> connection over stdin/stdout with all method handlers registered.
    /// </summary>
    internal static JsonRpc Create(IServiceProvider services, JsonSerializerOptions jsonOptions)
    {
        var formatter = new SystemTextJsonFormatter { JsonSerializerOptions = jsonOptions };
        var handler = new NewLineDelimitedMessageHandler(
            Console.OpenStandardOutput(),
            Console.OpenStandardInput(),
            formatter);
        var jsonRpc = new JsonRpc(handler)
        {
            CancelLocallyInvokedMethodsWhenConnectionIsClosed = true
        };

        var generatingActiveHandler = services.GetRequiredService<GeneratingActiveHandler>();
        var generatingCompletedHandler = services.GetRequiredService<GeneratingCompletedHandler>();
        var recipeHandler = services.GetRequiredService<RecipeHandler>();
        var summarizationHandler = services.GetRequiredService<SummarizationHandler>();
        var settingsHandler = services.GetRequiredService<SettingsHandler>();

        #region generator.active

        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.CreateAsync)),
            generatingActiveHandler, Attr("generator.active.create"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.StopAsync)),
            generatingActiveHandler, Attr("generator.active.stop"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.PauseAsync)),
            generatingActiveHandler, Attr("generator.active.pause"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.ResumeAsync)),
            generatingActiveHandler, Attr("generator.active.resume"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.StopAllAsync)),
            generatingActiveHandler, new JsonRpcMethodAttribute("generator.active.stopAll"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.PauseAllAsync)),
            generatingActiveHandler, new JsonRpcMethodAttribute("generator.active.pauseAll"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingActiveHandler>(nameof(GeneratingActiveHandler.ListAsync)),
            generatingActiveHandler, new JsonRpcMethodAttribute("generator.active.list"));

        #endregion

        #region generator.completed

        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingCompletedHandler>(nameof(GeneratingCompletedHandler.ListAsync)),
            generatingCompletedHandler, new JsonRpcMethodAttribute("generator.completed.list"));
        jsonRpc.AddLocalRpcMethod(GetMethod<GeneratingCompletedHandler>(nameof(GeneratingCompletedHandler.DeleteAsync)),
            generatingCompletedHandler, Attr("generator.completed.delete"));
        jsonRpc.AddLocalRpcMethod(
            GetMethod<GeneratingCompletedHandler>(nameof(GeneratingCompletedHandler.DeleteAllAsync)),
            generatingCompletedHandler, new JsonRpcMethodAttribute("generator.completed.deleteAll"));

        #endregion

        #region recipe

        jsonRpc.AddLocalRpcMethod(GetMethod<RecipeHandler>(nameof(RecipeHandler.ListAsync)),
            recipeHandler, new JsonRpcMethodAttribute("recipe.list"));
        jsonRpc.AddLocalRpcMethod(GetMethod<RecipeHandler>(nameof(RecipeHandler.QueryAsync)),
            recipeHandler, Attr("recipe.query"));
        jsonRpc.AddLocalRpcMethod(GetMethod<RecipeHandler>(nameof(RecipeHandler.AddAsync)),
            recipeHandler, new JsonRpcMethodAttribute("recipe.add"));
        jsonRpc.AddLocalRpcMethod(GetMethod<RecipeHandler>(nameof(RecipeHandler.UpdateAsync)),
            recipeHandler, new JsonRpcMethodAttribute("recipe.update"));
        jsonRpc.AddLocalRpcMethod(GetMethod<RecipeHandler>(nameof(RecipeHandler.DeleteAsync)),
            recipeHandler, Attr("recipe.delete"));
        jsonRpc.AddLocalRpcMethod(GetMethod<RecipeHandler>(nameof(RecipeHandler.ExportAsync)),
            recipeHandler, new JsonRpcMethodAttribute("recipe.export"));
        jsonRpc.AddLocalRpcMethod(GetMethod<RecipeHandler>(nameof(RecipeHandler.ImportAsync)),
            recipeHandler, new JsonRpcMethodAttribute("recipe.import"));

        #endregion

        #region summarization

        jsonRpc.AddLocalRpcMethod(GetMethod<SummarizationHandler>(nameof(SummarizationHandler.SummarizeWorkbookAsync)),
            summarizationHandler, Attr("summarization.workbook"));
        jsonRpc.AddLocalRpcMethod(
            GetMethod<SummarizationHandler>(nameof(SummarizationHandler.SummarizePresentationAsync)),
            summarizationHandler, Attr("summarization.presentation"));

        #endregion

        #region settings

        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.GetAsync)),
            settingsHandler, new JsonRpcMethodAttribute("settings.get"));
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.UpdateAsync)),
            settingsHandler, Attr("settings.update"));
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.ResetAsync)),
            settingsHandler, new JsonRpcMethodAttribute("settings.reset"));

        #endregion

        #region settings.performance

        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.GetPerformanceAsync)),
            settingsHandler, new JsonRpcMethodAttribute("settings.performance.get"));
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.UpdatePerformanceAsync)),
            settingsHandler, Attr("settings.performance.update"));
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.ResetPerformanceAsync)),
            settingsHandler, new JsonRpcMethodAttribute("settings.performance.reset"));

        #endregion

        #region settings.network

        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.GetNetworkAsync)),
            settingsHandler, new JsonRpcMethodAttribute("settings.network.get"));
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.UpdateNetworkAsync)),
            settingsHandler, Attr("settings.network.update"));
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.ResetNetworkAsync)),
            settingsHandler, new JsonRpcMethodAttribute("settings.network.reset"));

        #endregion

        jsonRpc.StartListening();
        return jsonRpc;
    }

    /// <summary>
    ///     Attaches the <see cref="ProgressCoalescer" /> to <see cref="GeneratingEventBus" />/
    ///     <see cref="LogNotifier" /> so Request/Job/Row progress and log lines are batched and forwarded
    ///     as <c>progress/request</c>/<c>progress/jobs</c>/<c>progress/rows</c>/<c>log/entries</c> notifications.
    /// </summary>
    internal static void AttachProgressCoalescer(IServiceProvider services, JsonRpc jsonRpc)
    {
        var eventBus = services.GetRequiredService<GeneratingEventBus>();
        var logNotifier = services.GetRequiredService<LogNotifier>();
        var coalescer = services.GetRequiredService<ProgressCoalescer>();
        coalescer.Attach(eventBus, logNotifier, jsonRpc);
    }

    private static JsonRpcMethodAttribute Attr(string name)
    {
        return new JsonRpcMethodAttribute(name) { UseSingleObjectParameterDeserialization = true };
    }

    private static MethodInfo GetMethod<T>(string name)
    {
        return typeof(T).GetMethod(name) ??
               throw new InvalidOperationException($"Method {name} not found on {typeof(T).Name}");
    }
}