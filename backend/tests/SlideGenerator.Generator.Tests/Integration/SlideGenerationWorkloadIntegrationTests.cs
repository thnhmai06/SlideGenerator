/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: SlideGenerationWorkloadIntegrationTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SlideGenerator.Cloud;
using SlideGenerator.Document;
using SlideGenerator.Document.Presentations;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Template;
using SlideGenerator.Document.Workbooks;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Jobs.Workloads;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Image;
using SlideGenerator.Image.Cropping;
using SlideGenerator.Image.FaceDetection;
using SlideGenerator.Image.Loading;
using SlideGenerator.Jobs.Engine;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Settings.Mutable;
using Xunit;

namespace SlideGenerator.Generator.Tests.Integration;

/// <summary>
///     Runs the real 4-phase <see cref="SlideGenerationWorkload" /> pipeline end to end against the
///     <c>Data.xlsx</c> / <c>Template.pptx</c> fixtures — real Syncfusion workbook/presentation I/O, no
///     mocked domain logic. Per CLAUDE.md's "What NOT to unit test", the phase bodies require a Syncfusion
///     license and real files, so this belongs here rather than in <c>Unit/</c>.
/// </summary>
/// <remarks>
///     Requires <c>SYNCFUSION_LICENSE_KEY</c> or a populated <c>.env</c> file — an unlicensed/trial
///     Syncfusion registration stamps a <c>"SyncfusionLicense"</c>-named watermark shape onto slide 1 on
///     every <c>output.Save()</c> call, which (since this workload saves multiple times per phase)
///     eventually crashes <c>Shapes.ToDictionary(...)</c> in <see cref="SlideGenerationWorkload" /> with a
///     duplicate-key <see cref="ArgumentException" />. <c>SlideGenerator.Document.csproj</c> pins the
///     Syncfusion package versions rather than floating <c>"*"</c> for exactly this reason — verify the
///     pinned version still validates before bumping it.
/// </remarks>
[Trait("Category", "Integration")]
public sealed class SlideGenerationWorkloadIntegrationTests : IDisposable
{
    private static readonly string WorkbookPath =
        Path.GetFullPath(Path.Combine("fixtures", "data", "Data.xlsx"));

    private static readonly string TemplatePath =
        Path.GetFullPath(Path.Combine("fixtures", "data", "Template.pptx"));

    private static readonly string FallbackImagePath =
        Path.GetFullPath(Path.Combine("fixtures", "faces", "single", "f_0.jpg"));

    private readonly string _tempDir;

    /// <summary>Prepares a scratch output directory for the generated presentation.</summary>
    public SlideGenerationWorkloadIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"slidegen-workload-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    /// <summary>
    ///     Runs all 4 phases (create output → create slides → fill text → fill images) against 3 real rows
    ///     of <c>Data.xlsx</c>, mapped onto <c>Template.pptx</c>'s <c>{{Provider}}</c>/<c>{{Type}}</c>/
    ///     <c>{{ShouldDownload}}</c>/<c>{{Url}}</c> placeholders (all nested inside PowerPoint groups) and
    ///     its grouped <c>Picture 7</c> image shape (filled from a local fallback image, so no
    ///     network/OpenCV involved). Asserts the job reaches <see cref="JobStatus.Complete" /> and the
    ///     output <c>.pptx</c> genuinely contains composed text and a real (non-degenerate) cropped image.
    /// </summary>
    [Fact(DisplayName = "INTEGRATION: full 4-phase job generation against real fixtures")]
    public async Task RunAsync_RealFixtures_ProducesCompletedOutputWithComposedTextAndImage()
    {
        var services = new ServiceCollection();
        services.AddDocumentServices();
        services.AddImageServices();
        // ponytail: the only RoiOption below is AnchorType.Image (center-crop), which never calls the face
        // detector, so a fake avoids paying for a real YuNet pool (native model load) just to satisfy DI.
        services.AddSingleton(Substitute.For<IFaceDetector>());
        await using var provider = services.BuildServiceProvider();

        var workload = new SlideGenerationWorkload(
            provider.GetRequiredService<IWorkbookOpener>(),
            provider.GetRequiredService<IPresentationOpener>(),
            provider.GetRequiredService<TextComposer>(),
            provider.GetRequiredService<ISmartCropper>(),
            provider.GetRequiredService<IImageLoader>(),
            Substitute.For<ICloudClient>(),
            Substitute.For<IHttpClientFactory>(),
            FakeSettingProvider(),
            Substitute.For<IEventBus>(),
            NullLogger<SlideGenerationWorkload>.Instance);

        var outputPath = Path.Combine(_tempDir, "output.pptx");
        var spec = new JobSpecification(
            WorkbookPath,
            "Data",
            null,
            new IndexRangeFilter(1, 3),
            TemplatePath,
            1,
            [
                new TextInstruction(new HashSet<string> { "Provider" }, [new ColumnIdentifier("Provider")]),
                new TextInstruction(new HashSet<string> { "Type" }, [new ColumnIdentifier("Type")]),
                new TextInstruction(new HashSet<string> { "ShouldDownload" }, [new ColumnIdentifier("ShouldDownload")]),
                new TextInstruction(new HashSet<string> { "Url" }, [new ColumnIdentifier("Url")])
            ],
            [
                new ImageInstruction(
                    new HashSet<ShapeIdentifier> { new("Picture 7") },
                    [],
                    new ImageEditInstruction([new AnchorOption { Type = AnchorType.Image }]),
                    FallbackImagePath)
            ],
            outputPath);
        var initial = new JobSnapshot("req-1", 0, JobStatus.Pending, JobPhase.CreatingOutput, 0, spec,
            DateTimeOffset.UtcNow);

        var result = await workload.RunAsync(initial, new FakeJobContext(), TestContext.Current.CancellationToken);

        result.JobStatus.Should().Be(JobStatus.Complete);
        result.Phase.Should().Be(JobPhase.Done);
        result.CurrentIndex.Should().Be(3);
        File.Exists(outputPath).Should().BeTrue();

        using var output = await provider.GetRequiredService<IPresentationOpener>()
            .OpenPresentationReadOnlyAsync(new PresentationIdentifier(outputPath),
                TestContext.Current.CancellationToken);
        output.SlidesCount.Should().Be(3);

        var firstSlide = output.Slides.First();
        var shapes = firstSlide.Shapes.ToList();
        var allText = string.Concat(shapes.Select(s => s.DisplayText));
        allText.Should().NotContain("{{", "every Mustache placeholder must have been resolved");
        allText.Should().Contain("Google Drive", "row 1's Provider column must have been composed in");

        var pictureShape = shapes.FirstOrDefault(s => s.Identifier == new ShapeIdentifier("Picture 7"));
        pictureShape.Should().NotBeNull("the grouped image shape must be reachable by identifier");
        pictureShape!.ImageData.Should().NotBeNullOrEmpty("the fallback image must have been cropped and assigned");
        pictureShape.Bounds.Width.Should().BeGreaterThan(50,
            "Bounds must report real pixel dimensions, not the near-zero value from the old EMU/points unit bug");
    }

    private static ISettingProvider FakeSettingProvider()
    {
        var provider = Substitute.For<ISettingProvider>();
        provider.Current.Returns(new Setting());
        return provider;
    }

    /// <summary>Minimal <see cref="IJobContext{TState}" /> — never resuming, never paused, records nothing.</summary>
    private sealed class FakeJobContext : IJobContext<JobSnapshot>
    {
        public bool IsResume => false;

        public Task ReportAsync(JobSnapshot state, bool durable = false, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task CheckpointAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
