/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: HeadlessTestSession.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia.Headless;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     The one headless Avalonia app session shared by every test that needs <c>Application.Current</c>
///     (<see cref="ViewConstructionTests" />, <see cref="ThemeServiceTests" />). <c>Application.Current</c> is
///     a process-wide singleton — each test class starting its own <see cref="HeadlessUnitTestSession" /> via
///     <c>HeadlessUnitTestSession.StartNew</c> races to initialize it when xunit runs those classes in
///     parallel, which is exactly what caused intermittent failures here before this was pulled out into one
///     shared instance.
/// </summary>
internal static class HeadlessTestSession
{
    /// <summary>Gets the shared session, started once for the whole test assembly.</summary>
    public static HeadlessUnitTestSession Instance { get; } = HeadlessUnitTestSession.StartNew(typeof(App));
}
