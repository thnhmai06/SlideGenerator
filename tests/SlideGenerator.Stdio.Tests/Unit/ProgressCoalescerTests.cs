/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio.Tests
 * File: ProgressCoalescerTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nerdbank.Streams;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Stdio.Implementations;
using StreamJsonRpc;
using Xunit;

namespace SlideGenerator.Stdio.Tests.Unit;

/// <summary>
///     End-to-end test of the real coalesce → flush → JSON-RPC notify path over an actual
///     <see cref="JsonRpc" /> connection (an in-memory duplex stream pair, no Syncfusion/sidecar needed).
///     Exercises three things no mocked test can: (1) System.Text.Json actually serializing the new
///     Request/Job/Row progress records + enums through the same <see cref="SystemTextJsonFormatter" />
///     configuration production uses, (2) <see cref="IJobsRepository.Flushed" /> actually driving a
///     <c>progress/jobs</c> notification, and (3) the <see cref="RequestPhase.ProcessingStarted" /> race
///     fix — it must not fire until every job announced via <c>AnnounceExpectedJobCount</c> has left
///     <see cref="JobStatus.Pending" />.
/// </summary>
public sealed class ProgressCoalescerTests : IAsyncDisposable
{
    private readonly List<string> _loggedWarnings = [];
    private readonly GeneratingEventBus _bus = new();
    private readonly JsonRpc _clientRpc;
    private readonly ProgressCoalescer _coalescer;
    private readonly LogNotifier _logNotifier = new();

    private readonly List<JobSnapshot> _receivedJobs = [];
    private readonly List<RequestProgress> _receivedRequests = [];
    private readonly FakeJobsRepository _repository = new();
    private readonly JsonRpc _serverRpc;

    /// <summary>
    ///     Wires the coalescer to an in-memory duplex stream pair and registers the notification
    ///     handlers that capture the payloads this test asserts on.
    /// </summary>
    public ProgressCoalescerTests()
    {
        _coalescer = new ProgressCoalescer(_repository, new CapturingLogger<ProgressCoalescer>(_loggedWarnings));

        var jsonOptions = JsonRpcBootstrap.BuildJsonSerializerOptions();
        var (serverStream, clientStream) = FullDuplexStream.CreatePair();

        _serverRpc = new JsonRpc(new NewLineDelimitedMessageHandler(
            serverStream, serverStream, new SystemTextJsonFormatter { JsonSerializerOptions = jsonOptions }));
        _clientRpc = new JsonRpc(new NewLineDelimitedMessageHandler(
            clientStream, clientStream, new SystemTextJsonFormatter { JsonSerializerOptions = jsonOptions }));

        // Registered against JsonElement (not a strongly typed List<T>) because the batch size varies per
        // flush — StreamJsonRpc's positional-parameter binding can't match a variable-arity JSON array to
        // a fixed-arity delegate signature. Deserializing manually still exercises the exact same
        // SystemTextJsonFormatter/JsonSerializerOptions production uses.
        _clientRpc.AddLocalRpcMethod("progress/jobs", (Action<JsonElement>)(payload =>
            _receivedJobs.AddRange(payload.Deserialize<List<JobSnapshot>>(jsonOptions)!)));
        _clientRpc.AddLocalRpcMethod("progress/request", (Action<JsonElement>)(payload =>
            _receivedRequests.AddRange(payload.Deserialize<List<RequestProgress>>(jsonOptions)!)));
        _clientRpc.StartListening();
        _serverRpc.StartListening();

        _coalescer.Attach(_bus, _logNotifier, _serverRpc);
    }

    private static JobSpecification DummySpec =>
        new("wb.xlsx", "Sheet1", null, null, "template.pptx", 1, [], [], "out.pptx");

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _coalescer.DetachAsync();
        _serverRpc.Dispose();
        _clientRpc.Dispose();
    }

    private static JobSnapshot Job(string requestId, int jobId, JobStatus jobStatus)
    {
        return new JobSnapshot(requestId, jobId, jobStatus, JobPhase.CreatingOutput, 0, DummySpec,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    ///     Waits (polling) until <paramref name="predicate" /> is true or the timeout elapses.
    /// </summary>
    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (predicate()) return;
            await Task.Delay(50);
        }
    }

    /// <summary>
    ///     Publishing a job's progress must enqueue it into <see cref="IJobsRepository" /> and, once
    ///     flushed, deliver a real <c>progress/jobs</c> JSON-RPC notification whose payload deserializes
    ///     back to the same status — proving STJ can round-trip <see cref="JobSnapshot" />/<see cref="JobStatus" />
    ///     through the production formatter configuration.
    /// </summary>
    [Fact]
    public async Task PublishJobProgress_AfterFlush_DeliversNotificationOverRealJsonRpc()
    {
        var requestId = Guid.NewGuid().ToString();
        _bus.Publish(Job(requestId, 1, JobStatus.Running));

        await WaitUntilAsync(() => _repository.Enqueued.Count > 0, TimeSpan.FromSeconds(3));
        _repository.Enqueued.Should().ContainSingle(j => j.JobId == 1);

        await WaitUntilAsync(() => _receivedJobs.Count > 0, TimeSpan.FromSeconds(3));
        _receivedJobs.Should().ContainSingle(j => j.JobId == 1 && j.JobStatus == JobStatus.Running,
            "logged warnings: " + string.Join(" | ", _loggedWarnings));
    }

    /// <summary>
    ///     <see cref="RequestPhase.ProcessingStarted" /> must not fire while any announced job is still
    ///     <see cref="JobStatus.Pending" />, even though the coalescer sees one job's transition at a time —
    ///     this is the race the plan flagged and <c>AnnounceExpectedJobCount</c> fixes.
    /// </summary>
    [Fact]
    public async Task ProcessingStarted_DoesNotFireUntilEveryAnnouncedJobLeavesPending()
    {
        var requestId = Guid.NewGuid().ToString();
        _bus.AnnounceExpectedJobCount(requestId, 2);
        _bus.Publish(Job(requestId, 1, JobStatus.Pending));
        _bus.Publish(Job(requestId, 2, JobStatus.Pending));
        _bus.Publish(Job(requestId, 1, JobStatus.Running));

        await WaitUntilAsync(() => _receivedJobs.Any(j => j is { JobId: 1, JobStatus: JobStatus.Running }),
            TimeSpan.FromSeconds(3));

        _receivedRequests.Should()
            .NotContain(r => r.RequestId == requestId && r.Phase == RequestPhase.ProcessingStarted);

        _bus.Publish(Job(requestId, 2, JobStatus.Running));

        await WaitUntilAsync(
            () => _receivedRequests.Any(r => r.RequestId == requestId && r.Phase == RequestPhase.ProcessingStarted),
            TimeSpan.FromSeconds(3));

        _receivedRequests.Should().Contain(r => r.RequestId == requestId && r.Phase == RequestPhase.ProcessingStarted);
    }

    /// <summary>Captures Warning+ log messages so an assertion failure can show why a notify/flush failed.</summary>
    private sealed class CapturingLogger<T>(List<string> sink) : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel < LogLevel.Warning) return;
            sink.Add($"{logLevel}: {formatter(state, exception)} {exception}");
        }
    }

    /// <summary>
    ///     Minimal in-memory <see cref="IJobsRepository" /> fake — <see cref="Enqueue" /> immediately raises
    ///     <see cref="Flushed" /> (no real buffering/timer), since this test exercises the coalescer's
    ///     wiring, not <c>BufferedRepository</c>'s own coalesce/flush behavior (covered separately).
    /// </summary>
    private sealed class FakeJobsRepository : IJobsRepository
    {
        public readonly List<JobSnapshot> Enqueued = [];

        public void Enqueue(JobSnapshot snapshot)
        {
            Enqueued.Add(snapshot);
            Flushed?.Invoke([snapshot]);
        }

        public Task FlushAsync(CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public event Action<IReadOnlyList<JobSnapshot>>? Flushed;

        public Task<IReadOnlyList<JobSnapshot>> GetByRequestIdAsync(string requestId, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<JobSnapshot>>([.. Enqueued.Where(j => j.RequestId == requestId)]);
        }

        public Task<IReadOnlyList<JobSnapshot>> GetNonTerminalAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<JobSnapshot>>([]);
        }

        public Task<IReadOnlyList<JobSnapshot>> GetAllAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<JobSnapshot>>([.. Enqueued]);
        }

        public Task DeleteByRequestIdAsync(string requestId, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}