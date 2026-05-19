/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator
 * File: ICoordinator.cs
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

using SlideGenerator.Coordinator.Domain.Models;

namespace SlideGenerator.Coordinator.Application.Abstractions;

/// <summary>
///     Deduplicates expensive per-workflow asset operations (download/edit) across concurrent ForEach steps.
///     Each instance is scoped to exactly one workflow run; the first caller for a given key becomes
///     primary and performs the work, the following callers await the result then create a hard link.
/// </summary>
public interface ICoordinator
{
    /// <summary>
    ///     Enlist a key. First caller → <see cref="PrimaryEnlistment" /> (owns the operation, holds the
    ///     completion delegate). Subsequent callers → <see cref="SecondaryEnlistment" /> (await the result).
    /// </summary>
    Enlistment Enlist(string key);
}