/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: RowStage.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Generator.Progress;

/// <summary>Identifies a granular sub-action reported mid-execution via <see cref="RowProgress.Stage" />.</summary>
public enum RowStage
{
    None,
    Downloading,
    CroppingImage,
    SavingOutput
}