/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image
 * File: Registration.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Services;
using SlideGenerator.Image.Infrastructure.Adapters;
using SlideGenerator.Image.Infrastructure.Services;
using SmartCropper = SlideGenerator.Image.Application.Services.SmartCropper;

namespace SlideGenerator.Image.Injection;

/// <summary>
///     DI registration for the Image module.
/// </summary>
public static class Registration
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers all Image module services.</summary>
        public IServiceCollection AddImageServices()
        {
            services.AddSingleton<Func<IFaceDetector>>(_ => () => new YuNet());

            services.AddSingleton<IImageLoader, VipsImageLoader>();
            services.AddSingleton<IInterestCropper, LibvipsInterestCropper>();
            services.AddSingleton<IAnchorCropper>(sp => new AnchorCropper(
                sp.GetRequiredService<IFaceDetector>()));
            services.AddSingleton<ISmartCropper>(sp => new SmartCropper(
                sp.GetRequiredService<IAnchorCropper>(),
                sp.GetRequiredService<IInterestCropper>(),
                sp.GetService<ILogger<SmartCropper>>()));
            return services;
        }
    }
}