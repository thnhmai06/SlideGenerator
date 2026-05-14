/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: IAppLoggerFactory.cs
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
namespace SlideGenerator.Logging.Domain.Abstractions;

/// <summary>
///     Creates application loggers without exposing the underlying logging framework.
/// </summary>
public interface IAppLoggerFactory
{
    /// <summary>
    ///     Creates a normal logger that writes to a concrete log file path.
    /// </summary>
    /// <param name="name">The logical logger name.</param>
    /// <param name="logFilePath">The concrete file path where the logger writes.</param>
    /// <returns>The created logger.</returns>
    IAppLogger CreateLogger(string name, string logFilePath);

    /// <summary>
    ///     Creates a workflow logger that writes user-facing workflow progress to a concrete log file path.
    /// </summary>
    /// <param name="workflowId">The workflow instance identifier.</param>
    /// <param name="logFilePath">The concrete file path where the workflow logger writes.</param>
    /// <returns>The created workflow logger.</returns>
    IAppLogger CreateWorkflowLogger(string workflowId, string logFilePath);
}
