/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: SpreadsheetUtilitiesTests.cs
 */

using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using SlideGenerator.Document.Sheet;
using Syncfusion.XlsIO;
using Xunit;

namespace SlideGenerator.Tests.Document;

public sealed class SpreadsheetUtilitiesTests
{
    [Fact(Skip = "INTEGRATION: requires Syncfusion license and complex mocking of IWorksheet")]
    public void GetHeaders_ShouldReturnFirstRow()
    {
        // Logic: ws.GetRow(0)
    }

    [Fact(Skip = "INTEGRATION: requires Syncfusion license")]
    public void CountRows_ShouldHandleEmptyWorksheet()
    {
        // Mocking UsedRange as null
    }
}
