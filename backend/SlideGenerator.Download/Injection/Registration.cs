/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Download
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
using SlideGenerator.Download.Application.Abstractions;
using SlideGenerator.Download.Infrastructure.Services;

namespace SlideGenerator.Download.Injection;

public static class Registration
{
    public static IServiceCollection AddDownloadServices(this IServiceCollection services)
    {
        services.AddSingleton<IDownloadService, DownloadService>();
        return services;
    }
}





