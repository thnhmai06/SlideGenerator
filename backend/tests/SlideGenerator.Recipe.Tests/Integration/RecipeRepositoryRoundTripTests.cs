/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe.Tests
 * File: RecipeRepositoryRoundTripTests.cs
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

using Xunit;

namespace SlideGenerator.Recipe.Tests.Integration;

/// <summary>
///     Integration tests for full export/import round-trip of
///     <see cref="SlideGenerator.Recipe.Infrastructure.Services.RecipeRepository" />
///     using real workbook and presentation files bundled into the archive.
/// </summary>
public sealed class RecipeRepositoryRoundTripTests
{
    /// <summary>
    ///     INTEGRATION: Exporting a recipe that references real workbook and presentation files
    ///     must bundle those files into the archive. Re-importing the archive must restore the
    ///     files under the target directories and insert a new recipe row with the correct JSON.
    ///     TODO: provide fixtures at <c>tests/fixtures/recipe-roundtrip/source.xlsx</c> and
    ///     <c>tests/fixtures/recipe-roundtrip/source.pptx</c>.
    /// </summary>
    [Fact(
        DisplayName = "INTEGRATION: export+import round-trip bundles and restores workbook+presentation — TODO fixture",
        Skip = "TODO: provide fixtures at tests/fixtures/recipe-roundtrip/")]
    public Task Export_WithWorkbooksAndPresentations_BundlesAllFiles_AndImportRestores()
    {
        Skip.Always(
            "TODO: provide real .xlsx + .pptx at tests/fixtures/recipe-roundtrip/; see plan h-ng-x-l-keen-pelican.md");
        return Task.CompletedTask;
    }
}
