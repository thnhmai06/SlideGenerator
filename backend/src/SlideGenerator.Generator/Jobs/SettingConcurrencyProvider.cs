/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: SettingConcurrencyProvider.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Jobs.Engine;
using SlideGenerator.Settings.Mutable;

namespace SlideGenerator.Generator.Jobs;

/// <summary>Reads <c>Performance.MaxConcurrentJobs</c> for the generic job engine, re-read on every access.</summary>
internal sealed class SettingConcurrencyProvider(ISettingProvider settingProvider) : IJobConcurrencyProvider
{
    /// <inheritdoc />
    public int MaxConcurrentJobs => (int)settingProvider.Current.Performance.MaxConcurrentJobs;
}