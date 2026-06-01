/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: Registration.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Recipe.Application.Abstractions;
using SlideGenerator.Recipe.Infrastructure.Services;
using SlideGenerator.Settings.Domain.Rules;

namespace SlideGenerator.Recipe.Injection;

/// <summary>
///     Provides extension methods to register recipe services into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Adds recipe repository and package services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddRecipeServices(this IServiceCollection services)
    {
        services.AddSingleton(new SqliteConnectionStringBuilder(NameAndPaths.RecipesFile.ConnectionString));
        services.AddSingleton<IRecipeFileManifestExtractor, NullRecipeFileManifestExtractor>();
        services.AddSingleton<IRecipeRepository>(sp => new RecipeRepository(
            sp.GetRequiredService<SqliteConnectionStringBuilder>(),
            sp.GetRequiredService<IRecipeFileManifestExtractor>()));
        return services;
    }
}