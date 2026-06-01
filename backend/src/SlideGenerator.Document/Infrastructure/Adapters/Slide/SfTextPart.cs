/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SfTextPart.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Syncfusion.Presentation;

namespace SlideGenerator.Document.Infrastructure.Adapters.Slide;

internal class SfTextPart(ITextPart core)
    : Domain.Abstractions.Slide.ITextPart
{
    internal readonly ITextPart Core = core;

    public string Text
    {
        get => Core.Text;
        set => Core.Text = value;
    }
}