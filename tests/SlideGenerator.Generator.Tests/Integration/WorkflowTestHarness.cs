/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: WorkflowTestHarness.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Serilog;
using Serilog.Events;
using SlideGenerator.Cloud;
using SlideGenerator.Document;
using SlideGenerator.Document.Presentations;
using SlideGenerator.Document.Presentations.Components;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Generator.Jobs;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Generator.Workflows;
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Image;
using SlideGenerator.Image.FaceDetection;
using SlideGenerator.Logging;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Settings.Database;
using SlideGenerator.Settings.Mutable;
using SlideGenerator.Workflows;
using SlideGenerator.Workflows.Engine;

namespace SlideGenerator.Generator.Tests.Integration;

/// <summary>
///     Shared fixture for Phase-A.3 Request -&gt; Workflow -&gt; Job integration tests. Wires the real
///     <see cref="WorkflowEngine{TKey,TState}" />, <see cref="SlideGenerationWorkflow" />, <see cref="JobExecutor" />,
///     <see cref="JobGate" />, <see cref="IJobsRepository" /> and <see cref="IWorkflowsRepository" /> over a
///     real (temp-file) SQLite database migrated via <see cref="DatabaseMigrator" />; only
///     <see cref="ICloudClient" /> / <see cref="IFaceDetector" /> are stubbed (network and native models, see
///     AGENTS.md "What NOT to unit test"). Used by <c>WorkflowConcurrencyIntegrationTests</c>,
///     <c>WorkflowControlIntegrationTests</c>, <c>WorkflowFailureIntegrationTests</c> and
///     <c>WorkflowResumeIntegrationTests</c>.
/// </summary>
/// <remarks>
///     <para>
///         Synchronization is via <see cref="TaskCompletionSource" />s — never <c>Task.Delay</c>. The observer
///         and event-bus record every transition with a monotonic timestamp; tests wait on the appropriate
///         <c>WaitFor*</c> helper (which itself uses bounded <c>WaitAsync</c> timeouts) to make assertions
///         without ever sleeping the thread.
///     </para>
///     <para>
///         A new harness owns a fresh SQLite file under <see cref="TempRoot" />; <see cref="DisposeAsync" />
///         cleanly cancels the engine and deletes that file. The same fixture can be re-built over the same
///         file via <see cref="OpenResumedOn" /> to exercise crash-resume (scenario d).
///     </para>
/// </remarks>
internal sealed class WorkflowTestHarness : IAsyncDisposable
{
    /// <summary>The two test fixtures are read-only and shared across every test.</summary>
    private static readonly string WorkbookPath =
        Path.GetFullPath(Path.Combine("fixtures", "data", "Data.xlsx"));

    private static readonly string TemplatePath =
        Path.GetFullPath(Path.Combine("fixtures", "data", "Template.pptx"));

    private readonly ServiceProvider _provider;
    private readonly SqliteConnection _anchor;

    private readonly RecordingObserver _observer;
    private readonly RecordingEventBus _eventBus;
    private readonly RecordingLogNotifier _logNotifier;

    private readonly FakeSettingProvider _settings;
    private readonly bool _ownsTempRoot;
    private bool _disposed;

    /// <summary>Builds a fresh harness with its own temp SQLite file under the OS temp directory.</summary>
    public WorkflowTestHarness()
        : this(CreateFreshTempRoot(), ownsTempRoot: true)
    {
    }

    /// <summary>
    ///     Builds a harness against <paramref name="existingDbPath" /> (a file left behind by a previous harness).
    ///     The new harness does not own the temp root and does not delete it on dispose — it is the resumed
    ///     stack of scenario (d). Schema migrations are idempotent (<see cref="DatabaseMigrator" />), so calling
    ///     them on an already-migrated file is a no-op.
    /// </summary>
    private WorkflowTestHarness(string existingDbPath)
        : this(Path.GetDirectoryName(existingDbPath)!, ownsTempRoot: false, existingDbPath: existingDbPath)
    {
    }

    private WorkflowTestHarness(string tempRoot, bool ownsTempRoot, string? existingDbPath = null)
    {
        TempRoot = tempRoot;
        DbPath = existingDbPath ?? Path.Combine(tempRoot, "Data.db");
        _ownsTempRoot = ownsTempRoot;

        if (_ownsTempRoot)
        {
            Directory.CreateDirectory(TempRoot);
        }

        var builder = new SqliteConnectionStringBuilder { DataSource = DbPath };

        // Anchor keeps the (file-mode) database available across the short-lived per-operation connections
        // the repositories open. In-memory shared cache is not used so the file persists across harness
        // rebuilds (scenario d).
        _anchor = new SqliteConnection(builder.ConnectionString);
        _anchor.Open();

        DatabaseMigrator.Migrate(builder.ConnectionString);

        _observer = new RecordingObserver();
        _eventBus = new RecordingEventBus();
        _logNotifier = new RecordingLogNotifier();
        _settings = new FakeSettingProvider();

        var services = new ServiceCollection();
        // SerilogFileLoggerFactory takes a LoggerConfiguration (Transient). AddLoggingServices requires a
        // Transient LoggerConfiguration; the singleton file-logger factory builds one per log file. Provide a
        // minimal one matching the pattern in SlideGenerator.Desktop.App.ConfigureServices, just with Verbose
        // level so the test sees every line the file sink writes.
        services.AddTransient<LoggerConfiguration>(_ => new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .Enrich.FromLogContext());
        services.AddLoggingServices();
        services.AddCloudServices();
        services.AddDocumentServices();
        services.AddImageServices();
        // The only RoiOption used in these tests is AnchorType.Image (center crop) — never invokes the
        // face detector. Stubbing avoids paying for YuNet pool startup just to satisfy DI.
        services.AddSingleton(Substitute.For<IFaceDetector>());
        services.AddSingleton(_settings);
        services.AddSingleton<ISettingProvider>(_settings);
        // JobExecutor's Phase D network calls go through IHttpClientFactory; replace the registered
        // factory with a stub so we never reach the OS network. The job uses no images in these tests
        // (ImageInstructions are empty), so InspectSourcesAsync is a no-op — the stub is wired only to
        // satisfy DI.
        services.AddSingleton(Substitute.For<IHttpClientFactory>());

        services.AddSingleton(builder);
        services.AddSingleton<IWorkflowsRepository, WorkflowsRepository>();
        services.AddSingleton<IJobsRepository, JobsRepository>();
        services.AddSingleton<ILogFileReader, LogFileReader>();

        services.AddSingleton(sp =>
            new JobGate(() => (int)sp.GetRequiredService<FakeSettingProvider>().Current.Performance.MaxConcurrentJobs));
        services.AddSingleton<JobExecutor>();
        services.AddSingleton<SlideGenerationWorkflow>();
        // The engine's observer wires to two things in production: (a) the persistent workflow status
        // update (GeneratorWorkflowObserver -> WorkflowsRepository.UpdateStatusAsync) and (b) event-bus
        // publish for the UI. We want both: the persistent update must actually land in the DB so the
        // test can read back workflow rows, and a recording observer must fire the per-key terminal TCS
        // so the test can deterministically wait on terminal outcomes. A composite delegates to both.
        services.AddSingleton<GeneratorWorkflowObserver>();
        services.AddSingleton<IWorkflowObserver<string, WorkflowSnapshot>>(sp =>
            new CompositeObserver(
                _observer,
                sp.GetRequiredService<GeneratorWorkflowObserver>()));
        services.AddSingleton<IWorkflowResumeSource<string, WorkflowSnapshot>, GeneratorResumeSource>();
        services.AddSingleton(_eventBus);
        services.AddSingleton<IEventBus>(_eventBus);
        services.AddSingleton<ILogNotifier>(_logNotifier);
        services.AddWorkflowEngine<string, WorkflowSnapshot>();
        services.AddSingleton<IWorkflowController, WorkflowController>();

        _provider = services.BuildServiceProvider();
    }

    /// <summary>Root temp directory this harness owns — deleted on <see cref="DisposeAsync" />.</summary>
    public string TempRoot { get; }

    /// <summary>Full path to the SQLite database file this harness is wired against.</summary>
    public string DbPath { get; }

    /// <summary>The real <see cref="WorkflowEngine{TKey,TState}" /> the workflow controller delegates to.</summary>
    public IWorkflowEngine<string, WorkflowSnapshot> Engine =>
        _provider.GetRequiredService<IWorkflowEngine<string, WorkflowSnapshot>>();

    /// <summary>The workflow controller the harness hands to tests for start/pause/resume/stop calls.</summary>
    public IWorkflowController Controller => _provider.GetRequiredService<IWorkflowController>();

    /// <summary>Recorded workflow lifecycle transitions (progress / paused / resumed / terminal).</summary>
    public RecordingObserver Observer => _observer;

    /// <summary>The <see cref="JobGate" /> the workflow runner uses to cap concurrent jobs.</summary>
    public JobGate Gate => _provider.GetRequiredService<JobGate>();

    /// <summary>Fake settings — exposed so tests can flip <c>MaxConcurrentJobs</c> between calls.</summary>
    public FakeSettingProvider Settings => _settings;

    /// <summary>Recorded workflow/job/row events (publish-only sink; see <see cref="IEventBus" />).</summary>
    public RecordingEventBus EventBus => _eventBus;

    /// <summary>All captured log lines (one per <see cref="IFileLoggerFactory.CreateFile" /> callback).</summary>
    public RecordingLogNotifier LogNotifier => _logNotifier;

    /// <summary>Real repository — used by the resume scenario to read back rows directly.</summary>
    public IWorkflowsRepository Workflows => _provider.GetRequiredService<IWorkflowsRepository>();

    /// <summary>Real repository — used to read back job rows directly.</summary>
    public IJobsRepository Jobs => _provider.GetRequiredService<IJobsRepository>();

    /// <summary>
    ///     Opens a presentation read-only through the Document module — used by tests that need to inspect a
    ///     working (<c>.partial</c>) file the running job still holds open.
    /// </summary>
    public Task<IReadOnlyPresentation> OpenPresentationReadOnlyAsync(string path, CancellationToken ct = default)
    {
        return _provider.GetRequiredService<IPresentationOpener>()
            .OpenPresentationReadOnlyAsync(new PresentationIdentifier(path), ct);
    }

    /// <summary>
    ///     Builds a fresh harness that opens the same SQLite file as <paramref name="previous" /> —
    ///     the resume (scenario d) use case. <paramref name="previous" /> is NOT disposed by this method;
    ///     the test is responsible for disposing it once the resumed run has fully unwound.
    /// </summary>
    public static WorkflowTestHarness OpenResumedOn(WorkflowTestHarness previous)
    {
        return new WorkflowTestHarness(previous.DbPath);
    }

    /// <summary>
    ///     Whether a usable Syncfusion license key is present in the environment. Integration tests that need the
    ///     real <see cref="JobExecutor" /> phases skip themselves when this returns <see langword="false" />.
    /// </summary>
    public static bool HasLicense()
    {
        var key = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
        return !string.IsNullOrWhiteSpace(key) && key != "empty";
    }

    /// <summary>Builds the standard Data.xlsx + Template.pptx <see cref="JobSpecification" /> with the given row range.</summary>
    /// <param name="worksheetName">Worksheet name; <c>"NonExistent"</c> deliberately triggers a job-level failure.</param>
    /// <param name="rowRange">Optional inclusive 1-based row range; <see langword="null" /> means all rows.</param>
    /// <param name="outputPath">
    ///     Output <c>.pptx</c> path. Defaults to a unique file under <see cref="TempRoot" /> so multiple jobs of
    ///     one workflow can write to distinct files without colliding.
    /// </param>
    public JobSpecification BuildSpec(
        string worksheetName = "Data",
        IndexRangeFilter? rowRange = null,
        string? outputPath = null)
    {
        outputPath ??= Path.Combine(TempRoot, $"{Guid.NewGuid():N}.pptx");

        // Keep this JobSpecification aligned with JobExecutorIntegrationTests: it is the minimum set of
        // TextInstructions that exercises Phase C's Mustache composition against the fixture's placeholders.
        return new JobSpecification(
            WorkbookPath,
            worksheetName,
            null,
            rowRange,
            TemplatePath,
            1,
            [
                new TextInstruction(new HashSet<string> { "{{Provider}}" }, [new ColumnIdentifier("Provider")]),
                new TextInstruction(new HashSet<string> { "{{Type}}" }, [new ColumnIdentifier("Type")]),
                new TextInstruction(
                    new HashSet<string> { "{{ShouldDownload}}" },
                    [new ColumnIdentifier("ShouldDownload")]),
                new TextInstruction(new HashSet<string> { "{{Url}}" }, [new ColumnIdentifier("Url")])
            ],
            [],
            outputPath);
    }

    /// <summary>Persists the workflow row, then starts the workflow through the real controller.</summary>
    /// <returns>The new workflow id.</returns>
    public async Task<string> StartAsync(
        IReadOnlyList<JobSpecification> specs,
        string? name = null,
        CancellationToken ct = default)
    {
        var workflowId = Guid.NewGuid().ToString();
        var saveFolder = TempRoot;
        var logPath = Path.Combine(TempRoot, $"{workflowId}.log");

        var record = new WorkflowRecord(
            workflowId,
            new Request(1, name ?? $"wf-{workflowId[..8]}", PresentationType.Pptx, saveFolder),
            logPath,
            DateTimeOffset.UtcNow);
        await Workflows.CreateAsync(record, ct).ConfigureAwait(false);

        await Controller.StartWorkflowAsync(workflowId, specs, logPath, ct).ConfigureAwait(false);
        return workflowId;
    }

    /// <summary>Calls the engine's <c>InitializeAsync</c> via the resume source registered in DI.</summary>
    public Task InitializeAsync(CancellationToken ct = default) => Controller.InitializeAsync(ct);

    /// <summary>
    ///     Repeatedly reads <see cref="IJobsRepository.GetByWorkflowIdAsync" /> until <paramref name="predicate" />
    ///     matches the persisted jobs, or the timeout elapses. Polls with a tight 50 ms tick so the buffered
    ///     repository's ~1 s flush window does not dominate the wait; the test thread is always either
    ///     parked on a continuation or doing one short read.
    /// </summary>
    public async Task WaitForJobsAsync(
        string workflowId,
        Func<IReadOnlyList<JobSnapshot>, bool> predicate,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            // Force a flush so the buffered repo doesn't sit on stale rows.
            await Jobs.FlushAsync(CancellationToken.None).ConfigureAwait(false);

            var jobs = await Jobs.GetByWorkflowIdAsync(workflowId, ct).ConfigureAwait(false);
            if (jobs.Count > 0 && predicate(jobs))
            {
                return;
            }

            await Task.Delay(50, ct).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Predicate did not become true within {timeout}. Last jobs snapshot: " +
            string.Join(", ",
                (await Jobs.GetByWorkflowIdAsync(workflowId, ct).ConfigureAwait(false)).Select(j =>
                    $"({j.JobId},{j.JobStatus},{j.Phase},idx={j.CurrentIndex})")));
    }

    /// <summary>
    ///     Sets every job row of the workflow to <paramref name="status" /> straight in SQLite, bypassing
    ///     <see cref="IJobsRepository" /> whose upsert never rewinds a terminal row — used to reproduce the rows a
    ///     crash leaves behind.
    /// </summary>
    public async Task ForceJobStatusAsync(string workflowId, JobStatus status, CancellationToken ct = default)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = DbPath }.ConnectionString;
        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var command = conn.CreateCommand();
        command.CommandText = "UPDATE Jobs SET Status = $status WHERE WorkflowId = $id";
        command.Parameters.AddWithValue("$status", status.ToString());
        command.Parameters.AddWithValue("$id", workflowId);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Cancels the engine and waits for it to unwind (a graceful shutdown — never the crash scenario).</summary>
    public Task ShutdownAsync() => Controller.ShutdownAsync();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            await Controller.ShutdownAsync().ConfigureAwait(false);
        }
        catch
        {
            // Best-effort cleanup; the final dispose re-throws if anything else goes wrong.
        }

        await _provider.DisposeAsync().ConfigureAwait(false);

        _anchor.Dispose();

        if (_ownsTempRoot)
        {
            try
            {
                if (Directory.Exists(TempRoot))
                {
                    Directory.Delete(TempRoot, recursive: true);
                }
            }
            catch (IOException)
            {
                // Log files can be held briefly by Serilog sinks; ignore cleanup races.
            }
        }
    }

    private static string CreateFreshTempRoot()
    {
        return Path.Combine(Path.GetTempPath(), $"slidegen-wf-{Guid.NewGuid():N}");
    }

    #region Test doubles

    /// <summary>An <see cref="ISettingProvider" /> whose <c>MaxConcurrentJobs</c> tests can flip between calls.</summary>
    internal sealed class FakeSettingProvider : ISettingProvider
    {
        private readonly Lock _lock = new();
        private int _maxConcurrentJobs = 5;

        /// <inheritdoc />
        public Setting Current => new()
        {
            Performance = new Setting.PerformanceSetting { MaxConcurrentJobs = (uint)MaxConcurrentJobsValue }
        };

        private int MaxConcurrentJobsValue
        {
            get
            {
                lock (_lock)
                {
                    return _maxConcurrentJobs;
                }
            }
        }

        /// <summary>The current cap used by <see cref="JobGate" /> — must be ≥ 1.</summary>
        public int MaxConcurrentJobs
        {
            get
            {
                lock (_lock)
                {
                    return _maxConcurrentJobs;
                }
            }
            set
            {
                lock (_lock)
                {
                    _maxConcurrentJobs = Math.Max(1, value);
                }
            }
        }
    }

    /// <summary>
    ///     Captures every engine callback: progress reports are coalesced into a list and a terminal waiter is
    ///     exposed per key. Mirrors the shape of <c>SlideGenerator.Workflows.Tests.RecordingObserver</c> but
    ///     adapted to the string / <see cref="WorkflowSnapshot" /> pair this stack uses.
    /// </summary>
    internal sealed class RecordingObserver : IWorkflowObserver<string, WorkflowSnapshot>
    {
        private readonly Lock _lock = new();

        private readonly Dictionary<string, TaskCompletionSource<WorkflowTerminalResult<WorkflowSnapshot>>> _terminal =
            [];

        /// <summary>Every progress callback the engine forwarded (including the "starting execution" tick).</summary>
        public List<(string Key, WorkflowSnapshot State, bool Durable)> Progress { get; } = [];

        /// <summary>Every pause callback.</summary>
        public List<(string Key, WorkflowSnapshot State)> Paused { get; } = [];

        /// <summary>Every resume callback.</summary>
        public List<(string Key, WorkflowSnapshot State)> Resumed { get; } = [];

        /// <inheritdoc />
        public Task OnProgressAsync(string key, WorkflowSnapshot state, bool durable, CancellationToken ct)
        {
            lock (_lock)
            {
                Progress.Add((key, state, durable));
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnPausedAsync(string key, WorkflowSnapshot state, CancellationToken ct)
        {
            lock (_lock)
            {
                Paused.Add((key, state));
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnResumedAsync(string key, WorkflowSnapshot state, CancellationToken ct)
        {
            lock (_lock)
            {
                Resumed.Add((key, state));
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTerminalAsync(string key, WorkflowTerminalResult<WorkflowSnapshot> result, CancellationToken ct)
        {
            lock (_lock)
            {
                GetOrAddTcs(key).TrySetResult(result);
            }

            return Task.CompletedTask;
        }

        /// <summary>Returns a task that completes the next time the engine reports a terminal outcome for <paramref name="key" />.</summary>
        public Task<WorkflowTerminalResult<WorkflowSnapshot>> WaitTerminalAsync(string key)
        {
            lock (_lock)
            {
                return GetOrAddTcs(key).Task;
            }
        }

        private TaskCompletionSource<WorkflowTerminalResult<WorkflowSnapshot>> GetOrAddTcs(string key)
        {
            if (_terminal.TryGetValue(key, out var tcs)) return tcs;
            tcs = new TaskCompletionSource<WorkflowTerminalResult<WorkflowSnapshot>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _terminal[key] = tcs;
            return tcs;
        }
    }

    /// <summary>
    ///     Captures every <see cref="IEventBus" /> event (workflow / job / row). Tests can inspect
    ///     <see cref="PerWorkflowJobs" /> to build a per-workflow timeline without scanning the full list.
    /// </summary>
    internal sealed class RecordingEventBus : IEventBus
    {
        private readonly ConcurrentQueue<JobSnapshot> _jobs = [];
        private readonly ConcurrentQueue<WorkflowSnapshot> _workflows = [];
        private readonly ConcurrentQueue<RowProgress> _rows = [];

        /// <summary>Every <see cref="WorkflowSnapshot" /> the engine observer published.</summary>
        public IReadOnlyCollection<WorkflowSnapshot> Workflows => _workflows;

        /// <summary>Every <see cref="JobSnapshot" /> the job executor published.</summary>
        public IReadOnlyCollection<JobSnapshot> Jobs => _jobs;

        /// <summary>Every <see cref="RowProgress" /> published during Phase C / D.</summary>
        public IReadOnlyCollection<RowProgress> Rows => _rows;

        /// <summary>Jobs grouped by workflow id, in publish order (no de-dup, last write wins per terminal).</summary>
        public Dictionary<string, List<JobSnapshot>> PerWorkflowJobs()
        {
            var dict = new Dictionary<string, List<JobSnapshot>>();
            foreach (var j in _jobs)
            {
                if (!dict.TryGetValue(j.WorkflowId, out var list))
                {
                    list = [];
                    dict[j.WorkflowId] = list;
                }

                list.Add(j);
            }

            return dict;
        }

        /// <inheritdoc />
        public void Publish(WorkflowSnapshot workflow) => _workflows.Enqueue(workflow);

        /// <inheritdoc />
        public void Publish(JobSnapshot job) => _jobs.Enqueue(job);

        /// <inheritdoc />
        public void Publish(RowProgress progress) => _rows.Enqueue(progress);
    }

    /// <summary>Captures every <see cref="ILogNotifier" /> publish (one entry per Serilog file-sink event).</summary>
    internal sealed class RecordingLogNotifier : ILogNotifier
    {
        private readonly ConcurrentQueue<LogEntry> _entries = [];

        /// <summary>All log entries published in publish order.</summary>
        public IReadOnlyCollection<LogEntry> Entries => _entries;

        /// <inheritdoc />
        public void Publish(LogEntry entry) => _entries.Enqueue(entry);
    }

    /// <summary>
    ///     Fans engine callbacks out to two delegates: the recording observer (so the test can wait
    ///     deterministically on terminal outcomes) and the real <see cref="GeneratorWorkflowObserver" />
    ///     (so the workflow's persistent status update actually lands in the
    ///     <c>Workflows</c> table). Without the second leg, scenario (d)'s post-resume workflow-row read
    ///     would never observe <see cref="JobStatus.Complete" />.
    /// </summary>
    internal sealed class CompositeObserver(
        RecordingObserver observer1,
        IWorkflowObserver<string, WorkflowSnapshot> observer2)
        : IWorkflowObserver<string, WorkflowSnapshot>
    {
        /// <inheritdoc />
        public Task OnProgressAsync(string key, WorkflowSnapshot state, bool durable, CancellationToken ct) =>
            Task.WhenAll(
                observer1.OnProgressAsync(key, state, durable, ct),
                observer2.OnProgressAsync(key, state, durable, ct));

        /// <inheritdoc />
        public Task OnPausedAsync(string key, WorkflowSnapshot state, CancellationToken ct) =>
            Task.WhenAll(
                observer1.OnPausedAsync(key, state, ct),
                observer2.OnPausedAsync(key, state, ct));

        /// <inheritdoc />
        public Task OnResumedAsync(string key, WorkflowSnapshot state, CancellationToken ct) =>
            Task.WhenAll(
                observer1.OnResumedAsync(key, state, ct),
                observer2.OnResumedAsync(key, state, ct));

        /// <inheritdoc />
        public async Task OnTerminalAsync(string key, WorkflowTerminalResult<WorkflowSnapshot> result,
            CancellationToken ct)
        {
            // The persistent leg runs first: a test that awaits the recorder's terminal task then reads the
            // workflow row must observe the status the terminal callback wrote, not the previous one.
            await observer2.OnTerminalAsync(key, result, ct).ConfigureAwait(false);
            await observer1.OnTerminalAsync(key, result, ct).ConfigureAwait(false);
        }
    }

    #endregion
}