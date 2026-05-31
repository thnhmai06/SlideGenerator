/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Summarization
 * File: PreviewRule.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
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