/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: FaceTests.cs
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

using System.Drawing;
using FluentAssertions;
using SlideGenerator.Image.Models;
using Xunit;

namespace SlideGenerator.Tests.Image;

public sealed class FaceTests
{
    [Fact]
    public void FaceCenter_ShouldBeMiddleOfRect()
    {
        var face = new Face(new Rectangle(10, 20, 100, 200), 0.9f);
        face.FaceCenter.Should().Be(new Point(60, 120));
    }

    [Fact]
    public void EyesCenter_ShouldBeMiddleOfEyes()
    {
        var face = new Face(new Rectangle(0, 0, 100, 100), 0.9f,
            new Point(20, 30),
            new Point(40, 30));

        face.EyesCenter.Should().Be(new Point(30, 30));
    }

    [Fact]
    public void EyesCenter_ShouldBeNull_WhenOneEyeMissing()
    {
        var face = new Face(new Rectangle(0, 0, 100, 100), 0.9f, new Point(20, 30));
        face.EyesCenter.Should().BeNull();
    }

    [Fact]
    public void MouthCenter_ShouldBeMiddleOfMouthCorners()
    {
        var face = new Face(new Rectangle(0, 0, 100, 100), 0.9f,
            RightMouth: new Point(30, 70),
            LeftMouth: new Point(50, 70));

        face.MouthCenter.Should().Be(new Point(40, 70));
    }
}