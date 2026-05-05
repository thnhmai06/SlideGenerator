/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: ExceptionIdentifier.cs
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

namespace SlideGenerator.Logging.Models;

/// <summary>
///     A lightweight, serializable representation of an exception.
///     Avoids storing full exception objects which can cause serialization cycles or memory leaks.
/// </summary>
/// <param name="Name">The type name of the exception (e.g., System.IO.FileNotFoundException).</param>
/// <param name="Message">The error message describing the failure.</param>
public record ExceptionIdentifier(string Name, string Message);