/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: NullRecipeFileManifestExtractor.cs
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

using SlideGenerator.Recipe.Application.Abstractions;

namespace SlideGenerator.Recipe.Infrastructure.Services;

/// <summary>
///     Temporary stub implementation of <see cref="IRecipeFileManifestExtractor" /> that always
///     reports the manifest as unknown (returns <see langword="null" />). Consumers fall back to
///     "accept all paths that pass the extension whitelist and path-traversal guard".
///     Replace with a real parser once the ReactFlow JSON schema is finalized.
/// </summary>
internal sealed class NullRecipeFileManifestExtractor : IRecipeFileManifestExtractor
{
    /// <inheritdoc />
    public IReadOnlySet<string>? ExtractReferencedFiles(string recipeJson)
    {
        // TODO: replace with real parser once ReactFlow JSON schema is finalized.
        return null;
    }
}