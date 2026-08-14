/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Jobs
 * File: Registration.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Jobs.Engine;

namespace SlideGenerator.Jobs;

/// <summary>Registers a <see cref="IJobEngine{TKey,TState}" /> into the dependency injection container.</summary>
public static class Registration
{
    /// <summary>
    ///     Adds <see cref="JobEngine{TKey,TState}" /> as the <see cref="IJobEngine{TKey,TState}" /> for the
    ///     given <typeparamref name="TKey" />/<typeparamref name="TState" /> pair. The caller must separately
    ///     register <see cref="IJobConcurrencyProvider" /> and <see cref="IJobObserver{TKey,TState}" /> for
    ///     the same pair.
    /// </summary>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddJobEngine<TKey, TState>(this IServiceCollection services)
        where TKey : notnull
    {
        services.AddSingleton<IJobEngine<TKey, TState>, JobEngine<TKey, TState>>();
        return services;
    }
}
