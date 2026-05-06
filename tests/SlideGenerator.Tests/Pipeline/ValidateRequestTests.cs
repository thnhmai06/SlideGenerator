/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: ValidateRequestTests.cs
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Serilog;
using SlideGenerator.Coordinator.Models;
using SlideGenerator.Coordinator.Services;
using SlideGenerator.Document.Sheet.Models;
using SlideGenerator.Document.Slide.Models;
using SlideGenerator.Pipeline.Generating;
using SlideGenerator.Pipeline.Generating.Models;
using SlideGenerator.Pipeline.Generating.Steps;
using SlideGenerator.Pipeline.Generating.Workflows.Models;
using SlideGenerator.Settings.Services;
using Syncfusion.XlsIO;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;

namespace SlideGenerator.Tests.Pipeline;

public sealed class ValidateRequestTests
{
    private readonly Mock<GateLocker> _gateLockerMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly ExcelEngine _excelEngine;

    public ValidateRequestTests()
    {
        var settingProviderMock = new Mock<ISettingProvider>();
        var gateLoggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<GateLocker>>();
        
        // GateLocker is a custom singleton, we mock it as no-op for step tests
        _gateLockerMock = new Mock<GateLocker>(settingProviderMock.Object, gateLoggerMock.Object);
        _loggerMock = new Mock<ILogger>();
        _excelEngine = new ExcelEngine();

        // Ensure logger enrichment doesn't crash
        _loggerMock.Setup(x => x.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
            .Returns(_loggerMock.Object);
    }

    private (ValidateRequest step, IStepExecutionContext context, GeneratingTask data) BuildContext(GeneratingTask data)
    {
        var step = new ValidateRequest(_excelEngine, _gateLockerMock.Object, _loggerMock.Object);
        var contextMock = new Mock<IStepExecutionContext>();
        var workflowInstance = new WorkflowInstance
        {
            Data = data,
            Id = "test-workflow-id"
        };

        contextMock.SetupGet(c => c.Workflow).Returns(workflowInstance);

        return (step, contextMock.Object, data);
    }

    [Fact(Skip = "INTEGRATION: requires Syncfusion license and physical files")]
    public async Task RunAsync_ShouldReturnNext_WhenRequestIsValid()
    {
        // Arrange
        var sheet = new SheetIdentifier("valid.xlsx", "Sheet1");
        var slide = new SlideIdentifier("template.pptx", 1);
        var node = new MapNode(new HashSet<SheetIdentifier> { sheet }, slide, [], []);
        var request = new GeneratingRequest(new Recipe([node]), PresentationType.Pptx, "output");
        var data = new GeneratingTask { Request = request };

        var (step, context, _) = BuildContext(data);
        var stepWithItem = new ValidateRequest(_excelEngine, _gateLockerMock.Object, _loggerMock.Object)
        {
            Item = new ValidationItem(sheet, node)
        };

        // Act
        var result = await stepWithItem.RunAsync(context);

        // Assert
        result.Should().Be(ExecutionResult.Next());
        data.ValidWorksheets.Should().ContainKey(sheet);
    }

    [Fact]
    public async Task RunAsync_ShouldLogAndReturnNext_WhenExcelPathDoesNotExist()
    {
        // Arrange
        var sheet = new SheetIdentifier("nonexistent.xlsx", "Sheet1");
        var slide = new SlideIdentifier("template.pptx", 1);
        var node = new MapNode(new HashSet<SheetIdentifier> { sheet }, slide, [], []);
        var request = new GeneratingRequest(new Recipe([node]), PresentationType.Pptx, "output");
        var data = new GeneratingTask { Request = request };

        var (step, context, _) = BuildContext(data);
        var stepWithItem = new ValidateRequest(_excelEngine, _gateLockerMock.Object, _loggerMock.Object)
        {
            Item = new ValidationItem(sheet, node)
        };

        // Act
        var result = await stepWithItem.RunAsync(context);

        // Assert
        // Code catches FileNotFoundException, logs it, and continues
        result.Should().BeEquivalentTo(ExecutionResult.Next());
        data.ValidWorksheets.Should().BeEmpty();
        _loggerMock.Verify(x => x.Error(It.IsAny<FileNotFoundException>(), It.IsAny<string>()), Times.Once);
    }

    [Fact(Skip = "INTEGRATION: requires Syncfusion license and physical Excel file")]
    public async Task RunAsync_ShouldLogAndReturnNext_WhenSheetNotFoundInWorkbook()
    {
        // Arrange
        var sheet = new SheetIdentifier("valid.xlsx", "MissingSheet");
        var slide = new SlideIdentifier("template.pptx", 1);
        var node = new MapNode(new HashSet<SheetIdentifier> { sheet }, slide, [], []);
        var request = new GeneratingRequest(new Recipe([node]), PresentationType.Pptx, "output");
        var data = new GeneratingTask { Request = request };

        var (step, context, _) = BuildContext(data);
        var stepWithItem = new ValidateRequest(_excelEngine, _gateLockerMock.Object, _loggerMock.Object)
        {
            Item = new ValidationItem(sheet, node)
        };

        // Act
        var result = await stepWithItem.RunAsync(context);

        // Assert
        result.Should().Be(ExecutionResult.Next());
        data.ValidWorksheets.Should().BeEmpty();
    }
}
