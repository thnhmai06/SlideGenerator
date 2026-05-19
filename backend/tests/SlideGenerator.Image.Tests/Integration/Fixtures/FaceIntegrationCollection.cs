/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: FaceIntegrationCollection.cs
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

using Xunit;

namespace SlideGenerator.Image.Tests.Integration.Fixtures;

/// <summary>
///     xUnit v3 collection definition that groups integration tests requiring a downloaded face
///     dataset and real image-processing services (YuNet, MagickImage, OpenCV).
///     Both <see cref="FaceDatasetFixture" /> and <see cref="ImageServiceFixture" /> are shared
///     across all tests in the collection.
/// </summary>
[CollectionDefinition("FaceIntegration")]
public sealed class FaceIntegrationCollection
    : ICollectionFixture<FaceDatasetFixture>, ICollectionFixture<ImageServiceFixture>;