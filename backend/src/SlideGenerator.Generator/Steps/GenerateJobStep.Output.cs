/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GenerateJobStep.Output.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SlideGenerator.Document.Abstractions.Sheet;
using SlideGenerator.Document.Abstractions.Slide;
using SlideGenerator.Document.Models.Sheet;
using SlideGenerator.Document.Models.Slide;
using SlideGenerator.Generator.Models.Data;

namespace SlideGenerator.Generator.Steps;

public sealed partial class GenerateJobStep
{
    /// <summary>
    ///     Opens <paramref name="presentationId" /> in read-write mode and removes write protection if present.
    /// </summary>
    /// <param name="presentationId">Presentation to open.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Open a writable handle to the presentation.</returns>
    private async Task<IPresentation> OpenOutputAsync(PresentationIdentifier presentationId, CancellationToken ct)
    {
        _logger.LogInformation("Opening output: {OutputPath}", presentationId.PresentationPath);
        var presentation = await presentationProvider.OpenPresentationAsync(presentationId, ct).ConfigureAwait(false);
        if (presentation.IsWriteProtected) presentation.RemoveWriteProtection();
        return presentation;
    }

    /// <summary>
    ///     Copies the source template to <see cref="Specification" />.<see cref="JobSpecification.OutputPath" />, removes all slides,
    ///     and returns an open writable handle to the empty presentation.
    ///     The presentation inherits the template's theme and layout but contains no slides.
    /// </summary>
    /// <param name="templateId">Source presentation to copy from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Open a writable handle to the newly created empty output presentation.</returns>
    private async Task<IPresentation> CreateOutputAsync(
        PresentationIdentifier templateId,
        CancellationToken ct)
    {
        _logger.LogInformation("{Status}", $"Creating output: {Specification.OutputPath}");
        var dir = Path.GetDirectoryName(Specification.OutputPath);
        if (dir != null) Directory.CreateDirectory(dir);
        File.Copy(templateId.PresentationPath, Specification.OutputPath, overwrite: true);

        var outputId = new PresentationIdentifier(Specification.OutputPath, templateId.PresentationPassword);
        var presentation = await OpenOutputAsync(outputId, ct).ConfigureAwait(false);
        for (var i = presentation.SlidesCount - 1; i >= 0; i--)
            presentation.RemoveSlideAt(i);
        return presentation;
    }

    /// <summary>
    ///     Opens <paramref name="workbookIdentifier" /> read-only and resolves the worksheet named
    ///     <paramref name="sheetName" /> within it.
    /// </summary>
    /// <param name="workbookIdentifier">Workbook to open.</param>
    /// <param name="sheetName">Name of the worksheet to resolve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The open workbook handle (caller owns disposal) and the resolved worksheet, or <see langword="null" /> if not found.</returns>
    private async Task<(IReadOnlyWorkbook Workbook, IReadOnlyWorksheet? Worksheet)> OpenWorksheetAsync(
        WorkbookIdentifier workbookIdentifier, string sheetName, CancellationToken ct)
    {
        _logger.LogInformation("Opening workbook: {BookPath}", workbookIdentifier.BookPath);
        var workbook = await workbookProvider.OpenWorkbookReadOnlyAsync(workbookIdentifier, ct).ConfigureAwait(false);
        return (workbook, workbook.GetWorksheet(sheetName));
    }

    /// <summary>
    ///     Returns the cached template slide for this job, opening the source presentation once if not yet cached.
    ///     The returned slide is a detached clone stored in <see cref="TransientContext.TemplateSlides" />
    ///     keyed by <see cref="JobSpecification.OutputPath" />. Returns <see langword="null" /> if the slide index is out of range.
    /// </summary>
    /// <param name="templateIdentifier">An identifier to the source template presentation.</param>
    /// <param name="slideIdentifier">An identifier that slides in the template to use.</param>
    /// <param name="templateSlideCache">The template slides cache.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Cached <see cref="ISlide" /> clone, or <see langword="null" /> if not found.</returns>
    private async Task<ISlide?> LoadTemplateSlideAsync(
        PresentationIdentifier templateIdentifier,
        SlideIdentifier slideIdentifier,
        ConcurrentDictionary<string, ISlide> templateSlideCache,
        CancellationToken ct)
    {
        if (templateSlideCache.TryGetValue(Specification.OutputPath, out var cached))
            return cached;

        _logger.LogInformation("{Status}",
            $"Loading template slide: {templateIdentifier.PresentationPath} #{slideIdentifier.SlideIndex}");
        using var template = await presentationProvider
            .OpenPresentationReadOnlyAsync(templateIdentifier, ct).ConfigureAwait(false);

        var slideIndex = slideIdentifier.SlideIndex - 1;
        if (slideIndex < 0 || slideIndex >= template.SlidesCount)
            return null;

        var slide = template.Slides.ElementAt(slideIndex).Clone();
        templateSlideCache[Specification.OutputPath] = slide;
        return slide;
    }

    /// <summary>Saves <paramref name="output" />.</summary>
    private Task SaveOutputAsync(IPresentation output, CancellationToken ct)
    {
        output.Save();
        return Task.CompletedTask;
    }
}
