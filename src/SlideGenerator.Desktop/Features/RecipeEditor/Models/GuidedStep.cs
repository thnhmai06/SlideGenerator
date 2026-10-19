/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: GuidedStep.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Desktop.Features.RecipeEditor.Models;

/// <summary>
///     The four steps of Guided mode (plan §5.2.a) — no internal vocabulary ("Mapping"/"Instruction"/"Source")
///     is shown to the user in this mode, only step titles like "Mẫu slide"/"Dữ liệu". Guided reuses the exact
///     same panels Advanced does (<c>SlideCanvasView</c>/<c>TextBindingsView</c>/<c>WorksheetSourcesView</c>) —
///     this enum only drives which of them <c>RecipeEditorView</c> shows at a time, not a second ViewModel tree.
/// </summary>
public enum GuidedStep
{
    /// <summary>① Pick the template presentation + slide.</summary>
    Template = 1,

    /// <summary>② Pick the workbook + worksheet(s).</summary>
    Data,

    /// <summary>③ Confirm text-placeholder and image-shape bindings.</summary>
    Binding,

    /// <summary>④ Review the job count, then save (optionally + run).</summary>
    Review
}
