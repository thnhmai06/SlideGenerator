/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
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
using SlideGenerator.Document.Slide.Services;
using SlideGenerator.Documents;
using Syncfusion.Licensing;
using Syncfusion.XlsIO;
using TextComposer = SlideGenerator.Document.Slide.Services.TextComposer;

namespace SlideGenerator.Document;

/// <summary>
///     Provides extension methods to register document-related services.
/// </summary>
public static class Registration
{
    public static IServiceCollection AddDocumentServices(this IServiceCollection services)
    {
        var licenseKey = SyncfusionLicense.Key; // decoded from XOR-encoded bytes at build time
        if (!string.IsNullOrWhiteSpace(licenseKey) && licenseKey != "empty")
            SyncfusionLicenseProvider.RegisterLicense(licenseKey);

        services.AddSingleton<ExcelEngine>();
        services.AddSingleton<ImageComposer>();
        services.AddSingleton<TextComposer>();

        return services;
    }
}