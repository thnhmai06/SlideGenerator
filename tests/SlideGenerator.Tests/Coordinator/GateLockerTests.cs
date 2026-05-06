/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: GateLockerTests.cs
 */

using System;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly Mock<ISettingProvider> _settingProviderMock;
    private readonly Mock<ILogger<GateLocker>> _loggerMock;
    private readonly GateLocker _gateLocker;

    public GateLockerTests()
    {
        _settingProviderMock = new Mock<ISettingProvider>();
        _loggerMock = new Mock<ILogger<GateLocker>>();
        
        var settings = new Setting
        {
            Job = new Setting.JobSetting
            {
                MaxParallelReadWorkbook = 1,
                MaxParallelDownloadImage = 2
            }
        };
        
        _settingProviderMock.SetupGet(s => s.Current).Returns(settings);
        _gateLocker = new GateLocker(_settingProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task AcquireAsync_ShouldBeGrantedImmediately_WhenUnderLimit()
    {
        // Act
        Func<Task> act = async () => await _gateLocker.AcquireAsync(GateType.ReadWorkbook);

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
        Func<Task> act = async () => await _gateLocker.AcquireAsync(GateType.DownloadImage);
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
        cts.Cancel();

        // Assert
        await ((Func<Task>)(async () => await waitingTask)).Should().ThrowAsync<OperationCanceledException>();
    }
}
