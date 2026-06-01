/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator
 * File: Registration.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Application.Services;

namespace SlideGenerator.Coordinator.Injection;

/// <summary>
///     Provides extension methods to register the coordinator services into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Adds <see cref="ICoordinatorFactory" /> to the service collection.
    ///     <see cref="IGateLocker{TGate}" /> is intentionally not registered here — callers that require a
    ///     gate locker must register their own <see cref="IGateLocker{TGate}" /> instance with an appropriate
    ///     limit-resolver delegate for their specific gate enum.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddCoordinatorServices(this IServiceCollection services)
    {
        services.AddSingleton<ICoordinatorFactory, CoordinatorFactory>();
        return services;
    }
}