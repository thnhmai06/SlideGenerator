/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SfPresentationProvider.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Document.Domain.Abstractions.Slide;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Document.Infrastructure.Adapters.Slide;
using Syncfusion.Presentation;
using IPresentation = SlideGenerator.Document.Domain.Abstractions.Slide.IPresentation;

namespace SlideGenerator.Document.Infrastructure.Services;

/// <summary>
///     Implementation of <see cref="IPresentationProvider" />.
/// </summary>
internal sealed class SfPresentationProvider : IPresentationProvider
{
    /// <inheritdoc />
    public IPresentation OpenPresentation(PresentationIdentifier identifier)
    {
        var presentation = Presentation.Open(identifier.PresentationPath, identifier.PresentationPassword);
        return new SfPresentation(presentation, identifier);
    }

    /// <inheritdoc />
    public IReadOnlyPresentation OpenPresentationReadOnly(PresentationIdentifier identifier)
    {
        var fileStream =
            new FileStream(identifier.PresentationPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var presentation = Presentation.Open(fileStream, identifier.PresentationPassword);

        return new SfPresentation(presentation, identifier, fileStream);
    }
}