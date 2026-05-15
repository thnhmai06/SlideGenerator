/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Summarization
 * File: PreviewRule.cs
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

namespace SlideGenerator.Summarization.Domain.Rules;

/// <summary>
///     Contains rules related to preview configurations for summarization operations.
/// </summary>
public static class PreviewRule
{
    /// <summary>
    ///     Specifies the maximum number of rows to include in a worksheet preview.
    ///     This constant is used to limit the number of rows returned during preview generation
    ///     in summarization operations, ensuring that previews remain lightweight and efficient
    ///     while providing enough data for meaningful insight.
    /// </summary>
    public const uint MaxPreviewRows = 20;
}