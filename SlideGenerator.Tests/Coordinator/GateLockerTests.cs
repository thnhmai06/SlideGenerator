/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: GateLockerTests.cs
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

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SlideGenerator.Coordinator.Models;
using SlideGenerator.Coordinator.Services;
using SlideGenerator.Settings.Models;
using SlideGenerator.Settings.Services;
using Xunit;

namespace SlideGenerator.Tests.Coordinator;

public sealed class GateLockerTests
{
    private readonly GateLocker _gateLocker;

    public GateLockerTests()
    {
        var settingProviderMock = new Mock<ISettingProvider>();
        var loggerMock = new Mock<ILogger<GateLocker>>();

        var settings = new Setting
        {
            Job = new Setting.JobSetting
            {
                MaxParallelReadWorkbook = 1,
                MaxParallelDownloadImage = 2
            }
        };

        settingProviderMock.SetupGet(s => s.Current).Returns(settings);
        _gateLocker = new GateLocker(settingProviderMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task AcquireAsync_ShouldBeGrantedImmediately_WhenUnderLimit()
    {
        // Act
        var act = async () => await _gateLocker.AcquireAsync(GateType.ReadWorkbook);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AcquireAsync_ShouldBlock_WhenAtLimit()
    {
        // Arrange
        await _gateLocker.AcquireAsync(GateType.ReadWorkbook); // Limit is 1

        // Act
        var secondAcquire = _gateLocker.AcquireAsync(GateType.ReadWorkbook);

        // Assert
        secondAcquire.IsCompleted.Should().BeFalse();

        // Release first
        _gateLocker.Release(GateType.ReadWorkbook);

        // Wait for it to complete
        var delay = Task.Delay(500);
        var completedTask = await Task.WhenAny(secondAcquire.AsTask(), delay);
        completedTask.Should().Be(secondAcquire.AsTask());
    }

    [Fact]
    public async Task AcquireAsync_ShouldNotBlock_OnDifferentResources()
    {
        // Arrange
        await _gateLocker.AcquireAsync(GateType.ReadWorkbook); // Limit 1

        // Act & Assert
        var act = async () => await _gateLocker.AcquireAsync(GateType.DownloadImage);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AcquireAsync_ShouldThrowOperationCanceledException_WhenCancelledWhileWaiting()
    {
        // Arrange
        await _gateLocker.AcquireAsync(GateType.ReadWorkbook);
        using var cts = new CancellationTokenSource();

        // Act
        var waitingTask = _gateLocker.AcquireAsync(GateType.ReadWorkbook, cts.Token);
        await cts.CancelAsync();

        // Assert
        await ((Func<Task>)(async () => await waitingTask)).Should().ThrowAsync<OperationCanceledException>();
    }
}