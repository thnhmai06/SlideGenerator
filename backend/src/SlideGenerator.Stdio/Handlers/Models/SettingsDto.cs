/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: SettingsDto.cs
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