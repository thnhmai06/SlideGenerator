/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator
 * File: CoordinatorFactory.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Coordinator.Application.Abstractions;

namespace SlideGenerator.Coordinator.Application.Services;

/// <inheritdoc />
internal sealed class CoordinatorFactory : ICoordinatorFactory
{
    /// <inheritdoc />
    public ICoordinator Create()
    {
        return new Coordinator();
    }
}