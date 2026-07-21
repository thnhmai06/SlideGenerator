/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document.Tests
 * File: SlideIntegrationTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Document.Infrastructure.Services;
using Syncfusion.Licensing;
using Xunit;

namespace SlideGenerator.Document.Tests.Integration;

/// <summary>
///     Integration tests for <see cref="Domain.Abstractions.Slide.IReadOnlySlide" /> equality
///     and <see cref="Domain.Abstractions.Slide.IReadOnlyPresentation.IsWriteProtected" />.
///     Requires a valid Syncfusion license.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SlideIntegrationTests
{
    private static readonly string TemplatePath = Path.Combine("fixtures", "data", "Template.pptx");
    private readonly SfPresentationProvider _provider = new();

    /// <summary>Registers the Syncfusion license before each test.</summary>
    public SlideIntegrationTests()
    {
        SyncfusionLicenseProvider.RegisterLicense(SyncfusionLicense.Key);
    }

    /// <summary>
    ///     Verifies that <see cref="Domain.Abstractions.Slide.IReadOnlySlide.Equals(Domain.Abstractions.Slide.IReadOnlySlide)" />
    ///     returns <see langword="true" /> when the same slide is compared with itself (deterministic equality).
    /// </summary>
    [Fact]
    public async Task Equals_SameSlide_ReturnsTrue()
    {
        using var pres = await _provider.OpenPresentationReadOnlyAsync(new PresentationIdentifier(TemplatePath));
        var slide = pres.Slides.First();

        slide.Equals(slide).Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="Domain.Abstractions.Slide.IReadOnlySlide.Equals(Domain.Abstractions.Slide.IReadOnlySlide)" />
    ///     returns <see langword="false" /> when compared against <see langword="null" />.
    /// </summary>
    [Fact]
    public async Task Equals_NullOther_ReturnsFalse()
    {
        using var pres = await _provider.OpenPresentationReadOnlyAsync(new PresentationIdentifier(TemplatePath));
        var slide = pres.Slides.First();

        slide.Equals(null).Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="Domain.Abstractions.Slide.IReadOnlyPresentation.IsWriteProtected" />
    ///     returns <see langword="false" /> for the unprotected <c>Template.pptx</c> fixture.
    /// </summary>
    [Fact]
    public async Task IsWriteProtected_UnprotectedPresentation_ReturnsFalse()
    {
        using var pres = await _provider.OpenPresentationReadOnlyAsync(new PresentationIdentifier(TemplatePath));
        pres.IsWriteProtected.Should().BeFalse();
    }
}
