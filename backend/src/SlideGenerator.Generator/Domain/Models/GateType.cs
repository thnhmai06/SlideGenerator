/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GateType.cs
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