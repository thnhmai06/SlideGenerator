/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: DownloadImageIntegrationTests.cs
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

using Xunit;

namespace SlideGenerator.Generator.Tests.Integration;

/// <summary>
///     Integration tests for <see cref="SlideGenerator.Generator.Application.Steps.DownloadImage" />
///     covering idempotency and cancellation behavior (BUG-12, BUG-16).
/// </summary>
public sealed class DownloadImageIntegrationTests
{
    /// <summary>
    ///     INTEGRATION (BUG-16): When the output image already exists and is valid, the step must
    ///     skip the download without loading the entire file into managed heap. Verifies the
    ///     <c>imageFactory.Open(filePath)</c> overload is used instead of
    ///     <c>File.ReadAllBytesAsync</c> + <c>imageFactory.Open(byte[])</c>.
    ///     TODO: provide a real image file at <c>tests/fixtures/bug-16/large-image.jpg</c> (≥ 50 MB).
    /// </summary>
    [Fact(DisplayName =
        "INTEGRATION (BUG-16): existing valid image skips download without loading to heap — TODO fixture")]
    public Task RunPrimary_ExistingValidImage_SkipsDownload_NoMemoryBloat()
    {
        Assert.Fail(
            "TODO: provide real image at tests/fixtures/bug-16/large-image.jpg; see plan h-ng-x-l-keen-pelican.md");
        return Task.CompletedTask;
    }

    /// <summary>
    ///     INTEGRATION (BUG-12): Cancelling the <see cref="System.Threading.CancellationToken" />
    ///     while the step is waiting on <c>gateLocker.AcquireAsync</c> must propagate an
    ///     <see cref="OperationCanceledException" /> and not block indefinitely.
    /// </summary>
    [Fact(DisplayName =
        "INTEGRATION (BUG-12): cancellation during gate acquire propagates OperationCanceledException — TODO mock")]
    public Task RunPrimary_CancellationToken_AbortsAcquireAndDownload()
    {
        Assert.Fail(
            "TODO: wire up mock IDownloadService + saturated gate, then cancel CTS; see plan h-ng-x-l-keen-pelican.md");
        return Task.CompletedTask;
    }
}