/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cryptography
 * File: Registration.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Cryptography.Application.Abstractions;
using SlideGenerator.Cryptography.Application.Services;
using SlideGenerator.Cryptography.Infrastructure;

namespace SlideGenerator.Cryptography.Injection;

/// <summary>
///     Provides extension methods to register cryptography services into the DI container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Adds cryptography services to the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddCryptographyServices(this IServiceCollection services)
    {
        services.AddSingleton<IEncrypter, Aes256Encrypter>();
        services.AddSingleton<IHasher, Sha256Hasher>();
        services.AddSingleton<IHashPathRegistry, HashPathRegistry>();

        return services;
    }
}