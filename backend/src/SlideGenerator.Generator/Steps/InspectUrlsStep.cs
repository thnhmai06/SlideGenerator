/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: InspectUrlsStep.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using Serilog.Context;
using SlideGenerator.Cloud.Services;
using SlideGenerator.Cloud.Models;
using SlideGenerator.Document.Services;
using SlideGenerator.Generator.Abstractions;
using SlideGenerator.Generator.Models.Data;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Settings.Abstractions;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Steps;

/// <summary>
///     Inspects every image cell's source across ALL data rows of a single <see cref="Specification" /> up front,
///     recording every result into <see cref="JobPersistContext.InspectedUrls" /> — the dictionary
///     <see cref="GenerateJobStep" /> consults directly when downloading, instead of re-querying the
///     shared <see cref="ICache" /> itself. Every resolved URI is also touched on the shared
///     <see cref="ICache.DownloadedFiles" /> registry so its permanent temp-cache file's TTL clock resets.
///     A source already present in <see cref="JobPersistContext.InspectedUrls" /> is skipped on a re-run
///     (resume/retry) — it was already inspected. A source that previously failed (transient error) is
///     never recorded there, so it is naturally re-inspected instead of treated as done.
/// </summary>
public sealed class InspectUrlsStep(
    IWorkbookProvider workbookProvider,
    ICloudClient cloudClient,
    ICache cache,
    IHttpClientFactory httpClientFactory,
    ISettingProvider settingProvider) : StepBodyAsync
{
    /// <summary>The job to inspect.</summary>
    private JobSpecification Specification { get; set; } = null!;

    private ILogger _logger = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (JobContext)context.Workflow.Data;
        var ct = context.CancellationToken;
        using var requestScope = LogContext.PushProperty("RequestId", data.Persist.RequestId);
        using var jobScope = LogContext.PushProperty("JobId", context.Workflow.Id);
        Specification = data.Persist.Specification;
        _logger = data.Transient.LoggerFactory!.CreateLogger(nameof(InspectUrlsStep));
        var recipe = data.Persist.Recipe;

        var workbookNode = (WorkbookNode)recipe.Nodes[Specification.Source.WorkbookNodeId];
        var worksheetNode = (WorksheetNode)recipe.Nodes[Specification.Source.WorksheetNodeId];
        var mapNode = (MapNode)recipe.Nodes[Specification.MapNodeId];

        if (mapNode.ImageInstructions.Count == 0 || !File.Exists(workbookNode.Workbook.BookPath))
            return ExecutionResult.Next();

        var workbook = await workbookProvider
            .OpenWorkbookReadOnlyAsync(workbookNode.Workbook, ct)
            .ConfigureAwait(false);

        var worksheet = workbook.GetWorksheet(worksheetNode.Worksheet.SheetName);
        if (worksheet == null)
        {
            workbook.Dispose();
            return ExecutionResult.Next();
        }

        var headerToIndex = Utilities.BuildHeaderToIndexMap(worksheet);
        var dataCount = worksheet.RowCount - 1;
        var dataRows = worksheetNode.RowFilter?.GetIndices(dataCount) ?? Enumerable.Range(1, dataCount);

        // Gathers every distinct image cell source across the whole job up front, so it is inspected
        // with one batched cache lookup instead of one per row. A source already present in
        // data.Persist.InspectedUrls (from a prior run of this step, e.g., a resume/retry) is skipped —
        // it was already inspected. A source that previously failed is never recorded there (see
        // GetOrUpdateManyAsync), so it naturally falls through and gets re-inspected here.
        var sources = new HashSet<string>();
        foreach (var dataRow in dataRows)
        {
            var row = worksheet.GetRow(dataRow + 1);
            var rowValues = headerToIndex.ToDictionary(kv => kv.Key, kv => row[kv.Value]);
            foreach (var instruction in mapNode.ImageInstructions)
            {
                var source = Utilities.GetSource(rowValues, instruction.Columns);
                if (string.IsNullOrWhiteSpace(source)) continue;
                if (data.Persist.Request.AllowLocalPaths && File.Exists(source)) continue;
                if (data.Persist.InspectedUrls.ContainsKey(source)) continue;
                sources.Add(source);
            }
        }

        workbook.Dispose();
        if (sources.Count == 0) return ExecutionResult.Next();

        var inspectedSource = await cache.InspectedUrls.GetOrUpdateManyAsync(
            sources, InspectAsync, ct).ConfigureAwait(false);

        // Real-time record into Persist so GenerateJobStep never needs to touch the shared cache itself —
        // a source absent here (or mapped to null) means download is skipped for it.
        foreach (var (source, inspected) in inspectedSource)
            data.Persist.InspectedUrls[source] = inspected;

        // Touches every distinct resolved URI's permanent temp-cache entry in one batched writer — no
        // consumer count involved, just a TTL-clock reset so it survives until it naturally expires.
        var touchEntries = inspectedSource.Values
            .Where(inspected => inspected != null)
            .GroupBy(inspected => inspected!.Uri)
            .Select(g => (g.Key, g.First()!.Extension))
            .ToList();
        if (touchEntries.Count > 0)
            await cache.DownloadedFiles.TouchManyAsync(touchEntries, ct).ConfigureAwait(false);

        return ExecutionResult.Next();
    }

    /// <summary>
    ///     Resolves one source through <see cref="ICloudClient.InspectAsync" />, using a fresh
    ///     <see cref="HttpClient" /> created at the point of use (never shared/cached across calls),
    ///     retrying with truncated exponential backoff (<see cref="Utilities.ExecuteWithBackoffAsync{T}" />)
    ///     up to <c>Network.Retry.MaxRetries</c> times if a single attempt fails (returns <see langword="null" />).
    ///     A confirmed non-image (or unparsable) result is cached as negative; a transient failure that
    ///     survives all retries is never cached.
    /// </summary>
    private async Task<(bool Cacheable, ContentInfo? Value)> InspectAsync(string source, CancellationToken ct)
    {
        _logger.LogInformation("Resolving URL: {Source}", source);
        var urlStr = source.Contains("://") ? source : "https://" + source;
        if (!Uri.TryCreate(urlStr, UriKind.Absolute, out var parsedUri))
            return (true, null);

        using var httpClient = httpClientFactory.CreateHttpClientWithSetting(settingProvider);
        var info = await Utilities.ExecuteWithBackoffAsync(
            settingProvider.Current.Network.Retry.MaxRetries,
            TimeSpan.FromSeconds(settingProvider.Current.Network.Retry.MaxRetryDelay),
            () => cloudClient.InspectAsync(parsedUri, httpClient, ct),
            ct).ConfigureAwait(false);
        return info == null
            ? (false, null)
            : (true, info.IsImage() ? info : null);
    }
}