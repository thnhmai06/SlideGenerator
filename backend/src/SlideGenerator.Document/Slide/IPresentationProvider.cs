/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IPresentationProvider.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Slide;

/// <summary>
///     Defines the contract for opening PowerPoint presentations.
///     Hides the Syncfusion <c>IPresentation</c> lifecycle from callers.
/// </summary>
public interface IPresentationProvider
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
