/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: IScopeManager.cs
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
///     Maintains the current hierarchical logging scope.
/// </summary>
public interface IScopeManager
{
    /// <summary>
    ///     Gets the current scope path.
    /// </summary>
    string CurrentScope { get; }

    /// <summary>
    ///     Begins a nested scope for the current execution context.
    /// </summary>
    /// <param name="scope">The scope path or segment to apply.</param>
    /// <returns>A disposable handle that restores the previous scope when disposed.</returns>
    IDisposable BeginScope(string scope);
}



