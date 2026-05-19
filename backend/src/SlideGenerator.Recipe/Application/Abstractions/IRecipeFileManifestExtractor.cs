/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: IRecipeFileManifestExtractor.cs
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

namespace SlideGenerator.Recipe.Application.Abstractions;

/// <summary>
///     Parses a recipe ReactFlow JSON string and returns the zip-relative file paths that the
///     recipe legitimately references. Used by <c>ImportAsync</c> to whitelist which archive
///     entries to extract — entries not listed here are rejected as untrusted.
/// </summary>
/// <remarks>
///     Paths returned must be normalized to forward-slash separators and rooted under
///     <c>Workbooks/</c> or <c>Presentations/</c>. Subfolders are allowed.
/// </remarks>
public interface IRecipeFileManifestExtractor
{
    /// <summary>
    ///     Extracts the set of zip-relative paths referenced by the recipe.
    ///     Return an empty set if the recipe references no external files (text-only recipe).
    ///     Return <see langword="null" /> to indicate the manifest cannot be derived yet —
    ///     callers should treat this as "accept all" (back-compat for stubbed implementations).
    /// </summary>
    /// <param name="recipeJson">The recipe ReactFlow JSON string.</param>
    /// <returns>
    ///     The set of allowed entry names (e.g. <c>Workbooks/Q1/data.xlsx</c>), or
    ///     <see langword="null" /> if the manifest is not yet computable.
    /// </returns>
    IReadOnlySet<string>? ExtractReferencedFiles(string recipeJson);
}