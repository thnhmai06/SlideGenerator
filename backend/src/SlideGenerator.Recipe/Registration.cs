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
using SlideGenerator.Settings.Immutable;

namespace SlideGenerator.Recipe;

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
        services.AddSingleton(new SqliteConnectionStringBuilder(NameAndPaths.DataFolder.DataFile.ConnectionString));
        services.AddSingleton(typeof(IRecipeRepository),
            sp => new SqliteRecipeRepository(sp.GetRequiredService<SqliteConnectionStringBuilder>()));
        services.AddSingleton<IRecipePackageService, RecipePackageService>();
        return services;
    }
}