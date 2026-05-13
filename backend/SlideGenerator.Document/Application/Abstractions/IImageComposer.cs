/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IImageComposer.cs
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

using Syncfusion.Presentation;

namespace SlideGenerator.Document.Application.Abstractions;

/// <summary>
///     Replaces image content in presentation shapes.
/// </summary>
public interface IImageComposer
{
    /// <summary>
    ///     Replaces the image content of a shape.
    /// </summary>
    /// <param name="shape">The target shape.</param>
    /// <param name="imageStream">The image stream to apply.</param>
    void Replace(IShape shape, Stream imageStream);
}