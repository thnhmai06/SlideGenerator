/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Summarization
 * File: Registration.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Summarization.Application.Abstractions;
using SlideGenerator.Summarization.Infrastructure.Services;

namespace SlideGenerator.Summarization.Injection;

/// <summary>
///     Provides extension methods to register scanning services into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Adds summarization services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSummarizationServices(this IServiceCollection services)
    {
        services.AddSingleton<ISummarizationService, SummarizationService>();
        return services;
    }
}