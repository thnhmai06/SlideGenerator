/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SfPresentation.cs
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

using SlideGenerator.Document.Slide.Models;
using Syncfusion.Presentation;

namespace SlideGenerator.Document.Slide.Entities;

/// <summary>
///     Wraps a Syncfusion IPresentation and its FileStream for proper disposal and saving.
///     Utilizes lazy initialization to defer file access until the <see cref="Value" /> is accessed.
/// </summary>
public sealed class SfPresentation : IDisposable
{
    private readonly PresentationIdentifier _identifier;
    private readonly bool _isWritable;
    private readonly Lazy<IPresentation> _lazyValue;
    private FileStream? _fileStream;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SfPresentation" /> class.
    /// </summary>
    /// <param name="identifier">The identifier containing path and password info.</param>
    /// <param name="isWritable">Whether to open the presentation in read-write mode.</param>
    public SfPresentation(PresentationIdentifier identifier, bool isWritable = true)
    {
        _identifier = identifier;
        _isWritable = isWritable;
        _lazyValue = new Lazy<IPresentation>(InitializePresentation);
    }

    /// <summary>
    ///     Gets the underlying Syncfusion presentation handle.
    ///     Accessing this property triggers the lazy initialization and opens the file.
    /// </summary>
    public IPresentation Value => _lazyValue.Value;

    /// <summary>
    ///     Disposes of the presentation and any underlying file streams.
    ///     Only disposes the presentation if it was actually opened.
    /// </summary>
    public void Dispose()
    {
        if (_lazyValue.IsValueCreated)
            Value.Dispose();

        _fileStream?.Dispose();
    }

    /// <summary>
    ///     Performs the actual opening of the presentation file based on its access mode.
    /// </summary>
    /// <returns>The opened <see cref="IPresentation" />.</returns>
    private IPresentation InitializePresentation()
    {
        if (_isWritable) return Presentation.Open(_identifier.PresentationPath, _identifier.PresentationPassword);

        _fileStream = new FileStream(_identifier.PresentationPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Presentation.Open(_fileStream, _identifier.PresentationPassword);
    }

    /// <summary>
    ///     Saves the presentation to its original location if it has been initialized.
    ///     If the presentation was never accessed via <see cref="Value" />, this method does nothing.
    /// </summary>
    public void Save()
    {
        if (!_lazyValue.IsValueCreated) return;

        if (_fileStream == null)
            Value.Save(_identifier.PresentationPath);
        else
            Value.Save(_fileStream);
    }
}