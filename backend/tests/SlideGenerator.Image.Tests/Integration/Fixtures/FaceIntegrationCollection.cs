/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: FaceIntegrationCollection.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
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