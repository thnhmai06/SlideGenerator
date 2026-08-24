/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: PresentationOpener.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Presentations.Components;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Utilities;
using Syncfusion.Presentation;
using IPresentation = SlideGenerator.Document.Presentations.Components.IPresentation;

namespace SlideGenerator.Document.Presentations;

/// <summary>
///     Defines the contract for opening PowerPoint presentations.
///     Hides the Syncfusion <c>IPresentation</c> lifecycle from callers.
/// </summary>
public interface IPresentationOpener
{
    /// <summary>
    ///     Opens a presentation in <b>read-write</b> mode asynchronously.
    ///     If the file is locked by another process, waits for the lock to release via
    ///     <see cref="System.IO.FileSystemWatcher" /> before retrying.
    /// </summary>
    /// <param name="identifier">The presentation to open.</param>
    /// <param name="ct">Token to cancel the wait.</param>
    /// <returns>A handle wrapping the opened presentation.</returns>
    /// <exception cref="System.IO.FileNotFoundException">If the presentation file does not exist.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="ct" /> is canceled while waiting.</exception>
    Task<IPresentation> OpenPresentationAsync(PresentationIdentifier identifier,
        CancellationToken ct = default);

    /// <summary>
    ///     Opens a presentation in <b>read</b> mode asynchronously.
    ///     If the file is locked by another process, waits for the lock to release via
    ///     <see cref="System.IO.FileSystemWatcher" /> before retrying.
    /// </summary>
    /// <param name="identifier">The presentation to open.</param>
    /// <param name="ct">Token to cancel the wait.</param>
    /// <returns>A handle wrapping the opened presentation.</returns>
    /// <exception cref="System.IO.FileNotFoundException">If the presentation file does not exist.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="ct" /> is canceled while waiting.</exception>
    Task<IReadOnlyPresentation> OpenPresentationReadOnlyAsync(PresentationIdentifier identifier,
        CancellationToken ct = default);
}

/// <summary>
///     Implementation of <see cref="IPresentationOpener" />.
/// </summary>
internal sealed class SfPresentationOpener : IPresentationOpener
{
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

    private static SfPresentation CreatePresentationInstance(PresentationIdentifier identifier)
    {
        var presentation = Presentation.Open(identifier.PresentationPath, identifier.PresentationPassword);
        ConfigureFontFallback(presentation);
        return new SfPresentation(presentation, identifier);
    }

    private static SfPresentation CreatePresentationReadOnlyInstance(PresentationIdentifier identifier)
    {
        var fileStream =
            new FileStream(identifier.PresentationPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var presentation = Presentation.Open(fileStream, identifier.PresentationPassword);
        ConfigureFontFallback(presentation);

        return new SfPresentation(presentation, identifier, fileStream);
    }

    // Without a fallback/substitute font, resolving a font missing on this machine (or a script the
    // template's declared font can't render) throws a NullReferenceException deep inside
    // FontSettings.GetFont during text layout — surfaces as a crash from ISlide.GetPreview()'s
    // PresentationRenderer.ConvertToImage call, not from anything obviously font-related.
    private static void ConfigureFontFallback(Syncfusion.Presentation.IPresentation presentation)
    {
        presentation.PresentationRenderer = new global::Syncfusion.PresentationRenderer.PresentationRenderer();
        presentation.FontSettings.FallbackFonts.InitializeDefault();
        presentation.FontSettings.SubstituteFont += (_, args) => args.AlternateFontName = "Arial";
    }
}