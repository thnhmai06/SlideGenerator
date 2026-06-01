/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator
 * File: ICoordinatorFactory.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Coordinator.Application.Abstractions;

/// <summary>Creates a fresh <see cref="ICoordinator" /> instance scoped to one workflow run.</summary>
public interface ICoordinatorFactory
{
    /// <summary>Returns a new, empty coordinator for a single workflow run.</summary>
    ICoordinator Create();
}