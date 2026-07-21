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
using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Domain.Models.Data;
using SlideGenerator.Generator.Domain.Models.Enum;
using SlideGenerator.Stdio.Implementations;
using StreamJsonRpc;
using Xunit;

namespace SlideGenerator.Stdio.Tests.Unit;

/// <summary>
///     End-to-end test of the real coalesce → flush → JSON-RPC notify path over an actual
///     <see cref="JsonRpc" /> connection (an in-memory duplex stream pair, no Syncfusion/sidecar needed).
///     Exercises three things no mocked test can: (1) System.Text.Json actually serializing the new
///     Request/Job/Row progress records + enums through the same <see cref="SystemTextJsonFormatter" />
///     configuration production uses, (2) the coalescer's flush timer actually firing and delivering a
///     notification, and (3) the <see cref="RequestPhase.ProcessingStarted" /> race fix — it must not fire
///     until every job announced via <c>AnnounceExpectedJobCount</c> has left <see cref="Status.Pending" />.
/// </summary>
public sealed class ProgressCoalescerTests : IAsyncDisposable
{
    private readonly GeneratingEventBus _bus = new();
    private readonly LogNotifier _logNotifier = new();
    private readonly FakeStudioRepository _repository = new();
    private readonly ProgressCoalescer _coalescer;
    private readonly JsonRpc _serverRpc;
    private readonly JsonRpc _clientRpc;

    private readonly List<JobProgress> _receivedJobs = [];
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
            _receivedJobs.AddRange(payload.Deserialize<List<JobProgress>>(jsonOptions)!)));
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

    /// <summary>
    ///     Waits (polling) until <paramref name="predicate" /> is true or the timeout elapses — the flush
    ///     loop runs on a ~1s <see cref="System.Threading.PeriodicTimer" />, so a plain assertion right
    ///     after publishing would race it.
    /// </summary>
    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (predicate()) return;
            await Task.Delay(100);
        }
    }

    /// <summary>
    ///     Publishing two jobs' progress and letting one flush tick pass must deliver a real
    ///     <c>progress/jobs</c> JSON-RPC notification whose payload deserializes back to the same
    ///     statuses — proving STJ can actually round-trip <see cref="JobProgress" />/<see cref="Status" />
    ///     through the production formatter configuration, and that the flush timer fires end to end.
    /// </summary>
    [Fact]
    public async Task PublishJobProgress_AfterFlushTick_DeliversNotificationOverRealJsonRpc()
    {
        var requestId = Guid.NewGuid().ToString();
        _bus.Publish(new JobProgress { RequestId = requestId, JobId = "job-1", Status = Status.Running, Timestamp = DateTimeOffset.UtcNow });

        await WaitUntilAsync(() => _repository.UpsertedJobs.Count > 0, TimeSpan.FromSeconds(3));
        _repository.UpsertedJobs.Should().ContainSingle(j => j.JobId == "job-1");

        await WaitUntilAsync(() => _receivedJobs.Count > 0, TimeSpan.FromSeconds(3));
        _receivedJobs.Should().ContainSingle(j => j.JobId == "job-1" && j.Status == Status.Running,
            "logged warnings: " + string.Join(" | ", LoggedWarnings));
    }

    /// <summary>
    ///     <see cref="RequestPhase.ProcessingStarted" /> must not fire while any announced job is still
    ///     <see cref="Status.Pending" />, even though the coalescer only sees one job's transition per
    ///     flush tick — this is the race the plan flagged and the implementation fixes via
    ///     <c>AnnounceExpectedJobCount</c>.
    /// </summary>
    [Fact]
    public async Task ProcessingStarted_DoesNotFireUntilEveryAnnouncedJobLeavesPending()
    {
        var requestId = Guid.NewGuid().ToString();
        _bus.AnnounceExpectedJobCount(requestId, 2);
        _bus.Publish(new JobProgress { RequestId = requestId, JobId = "job-1", Status = Status.Pending, Timestamp = DateTimeOffset.UtcNow });
        _bus.Publish(new JobProgress { RequestId = requestId, JobId = "job-2", Status = Status.Pending, Timestamp = DateTimeOffset.UtcNow });
        _bus.Publish(new JobProgress { RequestId = requestId, JobId = "job-1", Status = Status.Running, Timestamp = DateTimeOffset.UtcNow });

        await WaitUntilAsync(() => _receivedJobs.Any(j => j.JobId == "job-1" && j.Status == Status.Running), TimeSpan.FromSeconds(3));

        _receivedRequests.Should().NotContain(r => r.RequestId == requestId && r.Phase == RequestPhase.ProcessingStarted);

        _bus.Publish(new JobProgress { RequestId = requestId, JobId = "job-2", Status = Status.Running, Timestamp = DateTimeOffset.UtcNow });

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

    /// <summary>Minimal in-memory <see cref="IStudioRepository" /> fake — records what would have been upserted, does nothing else.</summary>
    private sealed class FakeStudioRepository : IStudioRepository
    {
        public readonly List<JobProgress> UpsertedJobs = [];

        public Task UpsertRequestAsync(RequestProgress progress, CancellationToken ct = default) => Task.CompletedTask;

        public Task UpsertJobsAsync(IReadOnlyList<JobProgress> progress, CancellationToken ct = default)
        {
            UpsertedJobs.AddRange(progress);
            return Task.CompletedTask;
        }

        public Task UpsertRowsAsync(IReadOnlyList<RowProgress> progress, CancellationToken ct = default) => Task.CompletedTask;

        public Task<RequestProgress?> GetRequestAsync(string requestId, CancellationToken ct = default) =>
            Task.FromResult<RequestProgress?>(null);

        public Task<IReadOnlyDictionary<string, JobProgress>> GetJobsAsync(string requestId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<string, JobProgress>>(new Dictionary<string, JobProgress>());

        public Task<IReadOnlyDictionary<int, RowProgress>> GetRowsAsync(string requestId, string jobId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<int, RowProgress>>(new Dictionary<int, RowProgress>());

        public Task DeleteRequestAsync(string requestId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
