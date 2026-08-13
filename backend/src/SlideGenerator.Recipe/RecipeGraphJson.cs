/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RecipeGraphJson.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlideGenerator.Recipe;

/// <summary>
///     Shared <see cref="JsonSerializerOptions" /> for serializing a <see cref="Mappings.Recipe" /> graph —
///     used by <see cref="SqliteRecipeRepository" /> (DB column) and <see cref="RecipePackageService" />
///     (<c>Recipe.json</c> archive entry) so both stay byte-for-byte compatible.
/// </summary>
internal static class RecipeGraphJson
{
    public static readonly JsonSerializerOptions Options = Build();

    private static JsonSerializerOptions Build()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new ReadOnlySetJsonConverterFactory());
        return options;
    }
}
