/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: PresentationUtilitiesTests.cs
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

using FluentAssertions;
using Moq;
using SlideGenerator.Document.Slide;
using Syncfusion.Presentation;
using Xunit;

namespace SlideGenerator.Tests.Document;

public sealed class PresentationUtilitiesTests
{
    [Fact(Skip = "INTEGRATION: requires Syncfusion license")]
    public void GetBoundsF_ShouldConvertEmuToPixels()
    {
        // 9525 EMU = 1 Pixel
        var shapeMock = new Mock<IShape>();
        shapeMock.SetupGet(s => s.Left).Returns(9525.0);
        shapeMock.SetupGet(s => s.Top).Returns(19050.0);
        shapeMock.SetupGet(s => s.Width).Returns(9525.0 * 10);
        shapeMock.SetupGet(s => s.Height).Returns(9525.0 * 20);

        var bounds = shapeMock.Object.GetBoundsF();

        bounds.X.Should().Be(1.0f);
        bounds.Y.Should().Be(2.0f);
        bounds.Width.Should().Be(10.0f);
        bounds.Height.Should().Be(20.0f);
    }
}