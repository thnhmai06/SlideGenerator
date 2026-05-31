/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: SettingsDto.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Settings.Domain.Entities;

namespace SlideGenerator.Stdio.Handlers.Models;

/// <summary>
///     Wire envelope returned by <c>settings.get</c>. Wraps the persisted <see cref="Setting" />
///     with a runtime-only flag that tells the client when one or more encrypted fields could
///     not be decrypted on load (typically a cross-machine copy) and therefore need to be re-entered.
///     The flag is never persisted.
/// </summary>
/// <param name="Setting">The current settings payload.</param>
/// <param name="RequiresCredentialReentry">
///     <see langword="true" /> if the last load failed to decrypt at least one encrypted field
///     (e.g. proxy password). The affected fields have been cleared on <paramref name="Setting" />
///     and the client must prompt the user to provide them again.
/// </param>
public sealed record SettingsDto(Setting Setting, bool RequiresCredentialReentry);