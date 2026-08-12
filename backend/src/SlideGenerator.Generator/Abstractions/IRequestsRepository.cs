/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: IRequestsRepository.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Models.Data;

namespace SlideGenerator.Generator.Abstractions;

/// <summary>
///     Unbuffered store for the <c>Requests</c> table — written once at request creation and read back
///     for <c>Summary</c>/resume; never updated afterward, so no buffering is needed (mirrors
///     <c>RecipeRepository</c>'s direct-connection-per-operation pattern).
/// </summary>
public interface IRequestsRepository
{
    /// <summary>Inserts a new request row.</summary>
    Task CreateAsync(RequestRecord record, CancellationToken ct = default);

    /// <summary>Gets a request row by id, or <see langword="null" /> if not found.</summary>
    Task<RequestRecord?> GetAsync(string requestId, CancellationToken ct = default);

    /// <summary>Deletes a request row.</summary>
    Task DeleteAsync(string requestId, CancellationToken ct = default);
}
