/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GateType.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Generator.Domain.Models;

/// <summary>Identifies a named concurrency gate used during slide generation.</summary>
public enum GateType
{
    /// <summary>Gate for image download operations.</summary>
    DownloadImage,

    /// <summary>Gate for image editing operations.</summary>
    EditImage,

    /// <summary>Gate for slide editing operations.</summary>
    EditPresentation,

    /// <summary>Gate for workbook file read operations.</summary>
    ReadWorkbook,

    /// <summary>Gate for presentation file read operations.</summary>
    ReadPresentation
}