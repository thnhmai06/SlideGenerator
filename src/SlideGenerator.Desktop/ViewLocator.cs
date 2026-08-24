/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: ViewLocator.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SlideGenerator.Desktop;

/// <summary>
///     Maps a ViewModel instance to its View by naming convention — <c>Foo.Bar.XyzViewModel</c> resolves to
///     <c>Foo.Bar.XyzView</c> in the same namespace. Registered as the app's <c>Application</c>-level
///     <c>DataTemplates</c> entry so any <c>ContentControl</c> bound directly to a ViewModel (e.g.
///     <see cref="Shell.ShellViewModel.CurrentPage" />) renders its View automatically.
/// </summary>
public sealed class ViewLocator : IDataTemplate
{
    /// <inheritdoc />
    public Control? Build(object? param)
    {
        if (param is null) return null;

        var viewTypeName = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var viewType = Type.GetType(viewTypeName);
        if (viewType is null) return new TextBlock { Text = $"View not found: {viewTypeName}" };

        try
        {
            return (Control)Activator.CreateInstance(viewType)!;
        }
        catch (Exception ex)
        {
            // A template-build failure would otherwise render as blank content with no visible error —
            // Serilog.Log is the same sink App.axaml.cs writes to, so this shows up in the same log file.
            Serilog.Log.Error(ex, "ViewLocator failed to construct {ViewType}", viewType);
            return new TextBlock { Text = $"View construction failed: {viewType}\n{ex.Message}" };
        }
    }

    /// <inheritdoc />
    public bool Match(object? data)
    {
        return data is ObservableObject;
    }
}
