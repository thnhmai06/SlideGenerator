/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: CompositeDisposable.cs
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
namespace SlideGenerator.Logging.Infrastructure.Services;

/// <summary>
///     Disposes a fixed group of disposable handles as one logical scope handle.
/// </summary>
/// <param name="disposables">The disposable handles to release together.</param>
internal sealed class CompositeDisposable(params IDisposable[] disposables) : IDisposable
{
    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var t in disposables)
            t.Dispose();
    }
}
