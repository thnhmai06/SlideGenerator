/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator
 * File: Registration.cs
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Application.Services;
using SlideGenerator.Coordinator.Infrastructure.Services;
using SlideGenerator.Settings.Application.Abstractions;

namespace SlideGenerator.Coordinator.Injection;

public static class Registration
{
    public static IServiceCollection AddCoordinatorServices(this IServiceCollection services)
    {
        services.AddSingleton<IGateLocker>(sp => new GateLocker(
            sp.GetRequiredService<ISettingProvider>(),
            sp.GetService<ILogger<GateLocker>>()));
        services.AddSingleton<ICoordinatorFactory, CoordinatorFactory>();
        return services;
    }
}