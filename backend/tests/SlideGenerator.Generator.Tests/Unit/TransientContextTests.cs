/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: TransientContextTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Generator.Domain.Models.Data;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests locking the shape of <see cref="TransientContext" /> after <c>TransientCache</c> was
///     removed: <c>TemplateSlides</c> lives directly on it.
/// </summary>
public sealed class TransientContextTests
{
    /// <summary>Verifies <see cref="TransientContext.TemplateSlides" /> starts empty.</summary>
    [Fact]
    public void TemplateSlides_Default_IsEmpty()
    {
        var transient = new TransientContext();

        transient.TemplateSlides.Should().BeEmpty();
    }
}
