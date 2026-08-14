/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RecipePackageService.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Recipe.Services;

/// <summary>
///     Exports/imports a stored <see cref="RecipeEntry" /> as a <c>*.recipe</c> zip package, bundling the
///     referenced workbook/presentation files alongside the recipe. Reads/writes recipe rows through
///     <see cref="IRecipeRepository" /> only — owns no storage of its own.
/// </summary>
public interface IRecipePackageService
{
    /// <summary>
    ///     Exports a stored recipe as a package file.
    /// </summary>
    /// <param name="id">The id of the recipe to export.</param>
    /// <param name="outputPath">The full path to write the output file.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExportAsync(int id, string outputPath, CancellationToken ct = default);

    /// <summary>
    ///     Imports a package file, extracts its resources, and stores the recipe in the database.
    /// </summary>
    /// <param name="filePath">The full path to the package file.</param>
    /// <param name="saveFolders">Target directories for extracted workbook and presentation files.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The metadata of the newly imported recipe.</returns>
    Task<IRecipeMetadata> ImportAsync(
        string filePath,
        (string Workbooks, string Presentations) saveFolders,
        CancellationToken ct = default);
}

/// <inheritdoc cref="IRecipePackageService" />
/// <remarks>
///     Split across <c>RecipePackageService.Export.cs</c> and <c>RecipePackageService.Import.cs</c> — this
///     file only carries the public contract and the shared dependency.
/// </remarks>
internal sealed partial class RecipePackageService(IRecipeRepository recipeRepository) : IRecipePackageService;