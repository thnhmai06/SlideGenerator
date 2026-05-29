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
///     Deduplicates identical keyed operations across concurrent callers.
///     Each instance tracks which keys have been enlisted; the first caller for a given key
///     becomes the owner and performs the work, the following callers wait for the owner's result.
/// </summary>
public interface ICoordinator
{
    /// <summary>
    ///     Enlists a key.
    ///     First caller receives an <see cref="OwnerEnlistment" /> and must produce or fault the result.
    ///     Subsequent callers receive a <see cref="WaiterEnlistment" /> and await the owner's result.
    /// </summary>
    /// <param name="key">The deduplication key identifying the operation.</param>
    /// <returns>
    ///     An <see cref="OwnerEnlistment" /> for the first caller, or a <see cref="WaiterEnlistment" />
    ///     for every caller after the first.
    /// </returns>
    Enlistment Enlist(string key);
}