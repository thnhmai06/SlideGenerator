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

using SlideGenerator.Document.Services;
using SlideGenerator.Document.Adapters.Slide;
using SlideGenerator.Document.Models.Slide;
using SlideGenerator.Utilities;
using Syncfusion.Presentation;
using IPresentation = SlideGenerator.Document.Adapters.Slide.IPresentation;

namespace SlideGenerator.Document.Services;

/// <summary>
///     Implementation of <see cref="IPresentationProvider" />.
/// </summary>
internal sealed class SfPresentationProvider : IPresentationProvider
{
    private static SfPresentation CreatePresentationInstance(PresentationIdentifier identifier)
    {
        var presentation = Presentation.Open(identifier.PresentationPath, identifier.PresentationPassword);
        return new SfPresentation(presentation, identifier);
    }

    private static SfPresentation CreatePresentationReadOnlyInstance(PresentationIdentifier identifier)
    {
        var fileStream =
            new FileStream(identifier.PresentationPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var presentation = Presentation.Open(fileStream, identifier.PresentationPassword);

        return new SfPresentation(presentation, identifier, fileStream);
    }

    /// <inheritdoc />
    public async Task<IPresentation> OpenPresentationAsync(PresentationIdentifier identifier,
        CancellationToken ct = default)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return CreatePresentationInstance(identifier);
            }
            catch (IOException ex) when (FileAccessHelper.IsFileLockedException(ex))
            {
                _ = ex;
            }

            await FileAccessHelper.WaitForFileChangeAsync(identifier.PresentationPath, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyPresentation> OpenPresentationReadOnlyAsync(PresentationIdentifier identifier,
        CancellationToken ct = default)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return CreatePresentationReadOnlyInstance(identifier);
            }
            catch (IOException ex) when (FileAccessHelper.IsFileLockedException(ex))
            {
                _ = ex;
            }

            await FileAccessHelper.WaitForFileChangeAsync(identifier.PresentationPath, ct).ConfigureAwait(false);
        }
    }
}
