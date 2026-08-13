/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
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
using SlideGenerator.Document.Slides;
using SlideGenerator.Document.Template;
using SlideGenerator.Document.Workbooks;
using Syncfusion.Licensing;

namespace SlideGenerator.Document;

/// <summary>
///     Provides extension methods to register document-related services.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Registers document services and activates the Syncfusion license.
    ///     License validation warnings are emitted by <see cref="SfWorkbookOpener" /> and
    ///     <see cref="SfPresentationOpener" /> on first use via their injected loggers.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDocumentServices(this IServiceCollection services)
    {
        var licenseKey = SyncfusionLicense.Key;
        if (!string.IsNullOrWhiteSpace(licenseKey) && licenseKey != "empty")
            SyncfusionLicenseProvider.RegisterLicense(licenseKey);

        services.AddSingleton<IWorkbookOpener, SfWorkbookOpener>();
        services.AddSingleton<IPresentationOpener, SfPresentationOpener>();
        services.AddSingleton<ITemplateEngine>(sp => new MustacheEngine(
            sp.GetService<ILogger<MustacheEngine>>()));
        services.AddSingleton<TextComposer, TextComposer>();

        return services;
    }
}