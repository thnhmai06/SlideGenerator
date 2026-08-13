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
using SlideGenerator.Generator.Abstractions;
using SlideGenerator.Generator.Models.Data;
using SlideGenerator.Generator.Models.Enum;
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
///     <see cref="Status.Pending" />.
/// </summary>
public sealed class ProgressCoalescerTests : IAsyncDisposable
{
    private readonly GeneratingEventBus _bus = new();
    private readonly LogNotifier _logNotifier = new();
    private readonly FakeJobsRepository _repository = new();
    private readonly ProgressCoalescer _coalescer;
    private readonly JsonRpc _serverRpc;
    private readonly JsonRpc _clientRpc;

    private readonly List<JobRecord> _receivedJobs = [];
    private readonly List<RequestProgress> _receivedRequests = [];

    public readonly List<string> LoggedWarnings = [];

    public ProgressCoalescerTests()
    {
        _coalescer = new ProgressCoalescer(_repository, new CapturingLogger<ProgressCoalescer>(LoggedWarnings));

        var jsonOptions = JsonRpcBootstrap.BuildJsonSerializerOptions();
        var (serverStream, clientStream) = FullDuplexStream.CreatePair();

        _serverRpc = new JsonRpc(new NewLineDelimitedMessageHandler(
            serverStream, serverStream, new SystemTextJsonFormatter { JsonSerializerOptions = jsonOptions }));
        _clientRpc = new JsonRpc(new NewLineDelimitedMessageHandler(
            clientStream, clientStream, new SystemTextJsonFormatter { JsonSerializerOptions = jsonOptions }));

        // Registered against JsonElement (not a strongly-typed List<T>) because the batch size varies per
        // flush — StreamJsonRpc's positional-parameter binding can't match a variable-arity JSON array to
        // a fixed-arity delegate signature. Deserializing manually still exercises the exact same
        // SystemTextJsonFormatter/JsonSerializerOptions production uses.
        _clientRpc.AddLocalRpcMethod("progress/jobs", (Action<JsonElement>)(payload =>
            _receivedJobs.AddRange(payload.Deserialize<List<JobRecord>>(jsonOptions)!)));
        _clientRpc.AddLocalRpcMethod("progress/request", (Action<JsonElement>)(payload =>
            _receivedRequests.AddRange(payload.Deserialize<List<RequestProgress>>(jsonOptions)!)));
        _clientRpc.StartListening();
        _serverRpc.StartListening();

        _coalescer.Attach(_bus, _logNotifier, _serverRpc);
    }

    public async ValueTask DisposeAsync()
    {
        await _coalescer.DetachAsync();
        _serverRpc.Dispose();
        _clientRpc.Dispose();
    }

    private static JobSpecification DummySpec => new("wb.xlsx", "Sheet1", null, null, "template.pptx", 1, [], [], "out.pptx");

    private static JobRecord Job(string requestId, int jobId, Status status) =>
        new(requestId, jobId, status, JobPhase.CreatingOutput, 0, DummySpec, DateTimeOffset.UtcNow);

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
    ///     back to the same status — proving STJ can round-trip <see cref="JobRecord" />/<see cref="Status" />
    ///     through the production formatter configuration.
    /// </summary>
    [Fact]
    public async Task PublishJobProgress_AfterFlush_DeliversNotificationOverRealJsonRpc()
    {
        var requestId = Guid.NewGuid().ToString();
        _bus.Publish(Job(requestId, 1, Status.Running));

        await WaitUntilAsync(() => _repository.Enqueued.Count > 0, TimeSpan.FromSeconds(3));
        _repository.Enqueued.Should().ContainSingle(j => j.JobId == 1);

        await WaitUntilAsync(() => _receivedJobs.Count > 0, TimeSpan.FromSeconds(3));
        _receivedJobs.Should().ContainSingle(j => j.JobId == 1 && j.Status == Status.Running,
            "logged warnings: " + string.Join(" | ", LoggedWarnings));
    }

    /// <summary>
    ///     <see cref="RequestPhase.ProcessingStarted" /> must not fire while any announced job is still
    ///     <see cref="Status.Pending" />, even though the coalescer sees one job's transition at a time —
    ///     this is the race the plan flagged and <c>AnnounceExpectedJobCount</c> fixes.
    /// </summary>
    [Fact]
    public async Task ProcessingStarted_DoesNotFireUntilEveryAnnouncedJobLeavesPending()
    {
        var requestId = Guid.NewGuid().ToString();
        _bus.AnnounceExpectedJobCount(requestId, 2);
        _bus.Publish(Job(requestId, 1, Status.Pending));
        _bus.Publish(Job(requestId, 2, Status.Pending));
        _bus.Publish(Job(requestId, 1, Status.Running));

        await WaitUntilAsync(() => _receivedJobs.Any(j => j.JobId == 1 && j.Status == Status.Running), TimeSpan.FromSeconds(3));

        _receivedRequests.Should().NotContain(r => r.RequestId == requestId && r.Phase == RequestPhase.ProcessingStarted);

        _bus.Publish(Job(requestId, 2, Status.Running));

        await WaitUntilAsync(
            () => _receivedRequests.Any(r => r.RequestId == requestId && r.Phase == RequestPhase.ProcessingStarted),
            TimeSpan.FromSeconds(3));

        _receivedRequests.Should().Contain(r => r.RequestId == requestId && r.Phase == RequestPhase.ProcessingStarted);
    }

    /// <summary>Captures Warning+ log messages so an assertion failure can show why a notify/flush failed.</summary>
    private sealed class CapturingLogger<T>(List<string> sink) : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

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
        public readonly List<JobRecord> Enqueued = [];

        public void Enqueue(JobRecord record)
        {
            Enqueued.Add(record);
            Flushed?.Invoke([record]);
        }

        public Task FlushAsync(CancellationToken ct = default) => Task.CompletedTask;

        public event Action<IReadOnlyList<JobRecord>>? Flushed;

        public Task<IReadOnlyList<JobRecord>> GetByRequestIdAsync(string requestId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<JobRecord>>([.. Enqueued.Where(j => j.RequestId == requestId)]);

        public Task<IReadOnlyList<JobRecord>> GetNonTerminalAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<JobRecord>>([]);

        public Task<IReadOnlyList<JobRecord>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<JobRecord>>([.. Enqueued]);

        public Task DeleteByRequestIdAsync(string requestId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
