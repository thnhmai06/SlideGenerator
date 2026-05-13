/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: SerilogScopeManager.cs
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
using SlideGenerator.Logging.Domain.Abstractions;

namespace SlideGenerator.Logging.Infrastructure.Services;

/// <summary>
///     Tracks hierarchical log scopes in the current asynchronous execution context.
/// </summary>
internal sealed class SerilogScopeManager : IScopeManager
{
    private readonly AsyncLocal<ScopeNode?> _current = new();

    /// <inheritdoc />
    public string CurrentScope => _current.Value?.Path ?? "Global";

    /// <inheritdoc />
    public IDisposable BeginScope(string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        var previous = _current.Value;
        var normalized = Normalize(scope);
        var path = previous is null || normalized.Contains('/', StringComparison.Ordinal)
            ? normalized
            : $"{previous.Path}/{normalized}";

        _current.Value = new ScopeNode(path);
        return new ScopeHandle(this, previous);
    }

    /// <summary>
    ///     Normalizes a scope segment by trimming whitespace and slash separators.
    /// </summary>
    /// <param name="scope">The raw scope value.</param>
    /// <returns>The normalized scope value.</returns>
    private static string Normalize(string scope)
    {
        return scope.Trim().Trim('/');
    }

    /// <summary>
    ///     Represents a scope entry stored in the async-local scope chain.
    /// </summary>
    /// <param name="Path">The full hierarchical scope path.</param>
    private sealed record ScopeNode(string Path);

    /// <summary>
    ///     Restores the previous scope when the current scope is disposed.
    /// </summary>
    /// <param name="manager">The scope manager that owns the async-local state.</param>
    /// <param name="previous">The previous scope node to restore.</param>
    private sealed class ScopeHandle(SerilogScopeManager manager, ScopeNode? previous) : IDisposable
    {
        private int _disposed;

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            manager._current.Value = previous;
        }
    }
}


