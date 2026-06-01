/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: CloseAllHandles.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using SlideGenerator.Generator.Domain.Models.Contexts;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Application.Steps;

/// <summary>
///     Disposes of all long-lived workbook and presentation handles.
///     This step should be executed at the very end of the workflow.
/// </summary>
public sealed class CloseAllHandles : StepBody
{
    /// <inheritdoc />
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        var data = (GeneratingContext)context.Workflow.Data;
        var logger = data.LoggerFactory!.CreateLogger(nameof(CloseAllHandles));

        logger.LogInformation("Closing all workbook and presentation handles. Workflow complete.");

        data.Dispose();
        return ExecutionResult.Next();
    }
}