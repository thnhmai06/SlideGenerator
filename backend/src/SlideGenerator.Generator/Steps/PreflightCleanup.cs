/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: PreflightCleanup.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using Serilog.Context;
using SlideGenerator.Generator.Workflows;
using SlideGenerator.Generator.Models.Data;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Steps;

/// <summary>
///     Runs first in <see cref="Workflows.JobWorkflow" /> to overwrite any prior output file left by an
///     earlier run of this exact job. Only ever touches this job's own <see cref="JobSpecification.OutputPath" /> —
///     never its parent directory or sibling files, since other jobs may share the same output folder.
/// </summary>
public sealed class PreflightCleanup : StepBody
{
    /// <inheritdoc />
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        var data = (JobContext)context.Workflow.Data;
        using var requestScope = LogContext.PushProperty("RequestId", data.Persist.RequestId);
        using var jobScope = LogContext.PushProperty("JobId", context.Workflow.Id);
        var outputPath = data.Persist.Specification.OutputPath;
        var logger = data.Transient.LoggerFactory!.CreateLogger(nameof(PreflightCleanup));

        try
        {
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
                logger.LogInformation("Removed prior output file '{OutputPath}'", outputPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Failed to clean up prior output at '{OutputPath}'", outputPath);
        }

        return ExecutionResult.Next();
    }
}
