/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: ConfirmDialog.axaml.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SlideGenerator.Desktop.Services.Dialogs;

/// <summary>
///     A minimal yes/no confirmation window, shown modally by <see cref="DialogService" />. Plain
///     code-behind rather than MVVM — a dialog this small does not need a ViewModel of its own.
/// </summary>
public sealed partial class ConfirmDialog : Window
{
    /// <summary>Identifies the <see cref="Message" /> styled property.</summary>
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ConfirmDialog, string>(nameof(Message));

    /// <summary>Identifies the <see cref="ConfirmLabel" /> styled property.</summary>
    public static readonly StyledProperty<string> ConfirmLabelProperty =
        AvaloniaProperty.Register<ConfirmDialog, string>(nameof(ConfirmLabel));

    /// <summary>Identifies the <see cref="CancelLabel" /> styled property.</summary>
    public static readonly StyledProperty<string> CancelLabelProperty =
        AvaloniaProperty.Register<ConfirmDialog, string>(nameof(CancelLabel));

    /// <summary>Constructs the dialog and loads its XAML.</summary>
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets the body text shown above the action buttons.</summary>
    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>Gets or sets the confirm button's label.</summary>
    public string ConfirmLabel
    {
        get => GetValue(ConfirmLabelProperty);
        set => SetValue(ConfirmLabelProperty, value);
    }

    /// <summary>Gets or sets the cancel button's label.</summary>
    public string CancelLabel
    {
        get => GetValue(CancelLabelProperty);
        set => SetValue(CancelLabelProperty, value);
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
