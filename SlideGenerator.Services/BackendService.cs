using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Threading.Channels;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using SlideGenerator.Features.Configs.Contracts;
using SlideGenerator.Features.Configs.Entities;
using SlideGenerator.Features.Jobs;
using SlideGenerator.Features.Jobs.Entities.Jobs;
using SlideGenerator.Framework.Features.Sheet.Services;
using SlideGenerator.Framework.Features.Slide.Services.Presentation;
using SlideGenerator.Services.Generating.Models;
using SlideGenerator.Services.Generating.Services;
using SlideGenerator.Services.Scanning;
using SlideGenerator.Services.Scanning.Models.Sheets;
using SlideGenerator.Services.Scanning.Models.Slides;


#pragma warning disable CS8602

namespace SlideGenerator.Services;

/// <summary>
///     Orchestrates job lifecycle, queue processing, and persistence for slide generation jobs.
/// </summary>
public sealed class BackendService : IAsyncDisposable
{
    /// <summary>
    ///     Maximum number of queued jobs buffered in memory.
    /// </summary>
    private const int QueueCapacity = 128;

    /// <summary>
    ///     Limits concurrent workbook-level executions.
    /// </summary>
    private readonly SemaphoreSlim _bookSemaphore;

    /// <summary>
    ///     Runtime control flags for jobs (pause, resume, cancel).
    /// </summary>
    private readonly ConcurrentDictionary<Guid, JobControlState> _controlStates = new();

    /// <summary>
    ///     SQLite database file path for job persistence.
    /// </summary>
    private readonly string _dbPath;

    /// <summary>
    ///     Runtime generator for per-row slide operations.
    /// </summary>
    private readonly GenerateService _generateService;

    /// <summary>
    ///     Channel used to queue job identifiers for processing.
    /// </summary>
    private readonly Channel<Guid> _queue;

    /// <summary>
    ///     Limits concurrent worksheet executions across active jobs.
    /// </summary>
    private readonly SemaphoreSlim _sheetSemaphore;

    /// <summary>
    ///     Elsa workflow dispatcher for snapshot persistence.
    /// </summary>
    private readonly JobSnapshotWorkflowDispatcher _snapshotWorkflowDispatcher;

    /// <summary>
    ///     Cancellation source for the background queue worker.
    /// </summary>
    private readonly CancellationTokenSource _workerCts = new();

    /// <summary>
    ///     Background task that consumes and dispatches queued jobs.
    /// </summary>
    private readonly Task _workerTask;

    /// <summary>
    ///     Initializes queue workers, configuration, persistence, and startup job recovery.
    /// </summary>
    public BackendService(
        IConfigProvider configProvider,
        GenerateService generateService,
        JobSnapshotWorkflowDispatcher snapshotWorkflowDispatcher)
    {
        var config = configProvider.Current;
        _generateService = generateService;
        _snapshotWorkflowDispatcher = snapshotWorkflowDispatcher;

        _dbPath = Config.DatabasePath;
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath) ?? AppContext.BaseDirectory);
        InitializeDatabase();

        var maxBooks = Math.Max(1, config.Job.MaxConcurrentJobs);
        _bookSemaphore = new SemaphoreSlim(maxBooks, maxBooks);
        _sheetSemaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 2));
        _queue = Channel.CreateBounded<Guid>(new BoundedChannelOptions(QueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        ResumeJobsOnStartupAsync(_workerCts.Token).GetAwaiter().GetResult();
        _workerTask = Task.Run(() => ProcessQueueAsync(_workerCts.Token));
    }

    /// <summary>
    ///     Stops worker processing and disposes allocated resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _queue.Writer.TryComplete();
        await _workerCts.CancelAsync();
        try
        {
            await _workerTask;
        }
        catch (OperationCanceledException)
        {
        }

        await _generateService.DisposeAsync();
        _sheetSemaphore.Dispose();
        _bookSemaphore.Dispose();
        _workerCts.Dispose();
    }

    /// <summary>
    ///     Raised whenever a job snapshot is updated.
    /// </summary>
    public event Action<JobSnapshotEntity>? JobUpdated;

    /// <summary>
    ///     Scans a slide file and returns detected placeholders and image shape ids.
    /// </summary>
    public Task<Presentation> ScanSlideAsync(string filePath, CancellationToken cancellationToken)
    {
        return ScanService.ScanPresentationAsync(filePath, cancellationToken);
    }

    /// <summary>
    ///     Scans an excel file and returns worksheet header/record metadata.
    /// </summary>
    public Task<Workbook> ScanSheetAsync(string filePath, CancellationToken cancellationToken)
    {
        return ScanService.ScanWorkbookAsync(filePath, cancellationToken);
    }

    /// <summary>
    ///     Creates and enqueues a generation job.
    /// </summary>
    public async Task<Guid> CreateJobAsync(SlidesGenerateRequest request, CancellationToken cancellationToken)
    {
        ValidationService.ValidateRequest(request);

        var jobId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var requestJson = JsonSerializer.Serialize(request);

        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var insertJob = connection.CreateCommand();
        insertJob.CommandText = @"
INSERT INTO jobs(id, status, progress, created_at, updated_at, request_json, message)
VALUES($id, $status, $progress, $created, $updated, $request, $message);";
        insertJob.Parameters.AddWithValue("$id", jobId.ToString());
        insertJob.Parameters.AddWithValue("$status", nameof(JobStatusEntity.Pending));
        insertJob.Parameters.AddWithValue("$progress", 0d);
        insertJob.Parameters.AddWithValue("$created", now.ToString("O", CultureInfo.InvariantCulture));
        insertJob.Parameters.AddWithValue("$updated", now.ToString("O", CultureInfo.InvariantCulture));
        insertJob.Parameters.AddWithValue("$request", requestJson);
        insertJob.Parameters.AddWithValue("$message", "Queued");
        await insertJob.ExecuteNonQueryAsync(cancellationToken);

        foreach (var sheetName in ValidationService.ResolveSelectedSheets(request))
        {
            var outputPath = Path.Combine(request.SaveFolder, $"{EscapeFileName(sheetName)}.pptx");
            var insertSheet = connection.CreateCommand();
            insertSheet.CommandText = @"
INSERT INTO job_sheets(job_id, sheet_name, output_path, current_row, total_rows, status, error)
VALUES($jobId, $sheetName, $outputPath, 0, 0, $status, NULL);";
            insertSheet.Parameters.AddWithValue("$jobId", jobId.ToString());
            insertSheet.Parameters.AddWithValue("$sheetName", sheetName);
            insertSheet.Parameters.AddWithValue("$outputPath", outputPath);
            insertSheet.Parameters.AddWithValue("$status", nameof(JobStatusEntity.Pending));
            await insertSheet.ExecuteNonQueryAsync(cancellationToken);
        }

        _controlStates[jobId] = new JobControlState();
        await _queue.Writer.WriteAsync(jobId, cancellationToken);

        var snapshot = await GetJobAsync(jobId, cancellationToken);
        if (snapshot != null) RaiseJobUpdated(snapshot);
        return jobId;
    }

    /// <summary>
    ///     Retrieves a persisted job snapshot by identifier.
    /// </summary>
    public async Task<JobSnapshotEntity?> GetJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var jobCommand = connection.CreateCommand();
        jobCommand.CommandText = @"
SELECT status, progress, created_at, updated_at, message
FROM jobs
WHERE id = $id;";
        jobCommand.Parameters.AddWithValue("$id", jobId.ToString());

        await using var reader = await jobCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var status = Enum.Parse<JobStatusEntity>(reader.GetString(0), true);
        var progress = reader.GetDouble(1);
        var createdAt = DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture);
        var updatedAt = DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture);
        var message = reader.IsDBNull(4) ? null : reader.GetString(4);

        var sheets = new List<SheetCheckpointEntity>();
        var sheetCommand = connection.CreateCommand();
        sheetCommand.CommandText = @"
SELECT sheet_name, output_path, current_row, total_rows, status, error
FROM job_sheets
WHERE job_id = $jobId
ORDER BY sheet_name;";
        sheetCommand.Parameters.AddWithValue("$jobId", jobId.ToString());

        await using var sheetReader = await sheetCommand.ExecuteReaderAsync(cancellationToken);
        while (await sheetReader.ReadAsync(cancellationToken))
            sheets.Add(new SheetCheckpointEntity(
                sheetReader.GetString(0),
                sheetReader.GetString(1),
                sheetReader.GetInt32(2),
                sheetReader.GetInt32(3),
                Enum.Parse<JobStatusEntity>(sheetReader.GetString(4), true),
                sheetReader.IsDBNull(5) ? null : sheetReader.GetString(5)));

        return new JobSnapshotEntity(jobId, status, progress, createdAt, updatedAt, message, sheets);
    }

    /// <summary>
    ///     Lists all persisted jobs ordered by creation time descending.
    /// </summary>
    public async Task<IReadOnlyList<JobSnapshotEntity>> ListJobsAsync(CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM jobs ORDER BY created_at DESC;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            ids.Add(Guid.Parse(reader.GetString(0)));

        var snapshots = new List<JobSnapshotEntity>(ids.Count);
        foreach (var id in ids)
        {
            var snapshot = await GetJobAsync(id, cancellationToken);
            if (snapshot != null) snapshots.Add(snapshot);
        }

        return snapshots;
    }

    /// <summary>
    ///     Applies a control action to a job.
    /// </summary>
    public async Task ControlJobAsync(Guid jobId, JobControlEntity action, CancellationToken cancellationToken)
    {
        var state = _controlStates.GetOrAdd(jobId, _ => new JobControlState());
        switch (action)
        {
            case JobControlEntity.Pause:
                state.IsPaused = true;
                await UpdateJobStatusAsync(jobId, JobStatusEntity.Paused, "Paused", cancellationToken);
                break;
            case JobControlEntity.Resume:
                state.IsPaused = false;
                await UpdateJobStatusAsync(jobId, JobStatusEntity.Pending, "Queued for resume", cancellationToken);
                await _queue.Writer.WriteAsync(jobId, cancellationToken);
                break;
            case JobControlEntity.Cancel:
                state.IsCancelled = true;
                await UpdateJobStatusAsync(jobId, JobStatusEntity.Cancelled, "Cancelled", cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }

        var snapshot = await GetJobAsync(jobId, cancellationToken);
        if (snapshot != null) RaiseJobUpdated(snapshot);
    }

    /// <summary>
    ///     Continuously consumes queued jobs and dispatches book-level execution.
    /// </summary>
    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        await foreach (var jobId in _queue.Reader.ReadAllAsync(cancellationToken))
        {
            var snapshot = await GetJobAsync(jobId, cancellationToken);
            if (snapshot == null) continue;
            if (snapshot.Status is JobStatusEntity.Completed or JobStatusEntity.Cancelled) continue;

            var state = _controlStates.GetOrAdd(jobId, _ => new JobControlState());
            if (state.IsCancelled) continue;

            await _bookSemaphore.WaitAsync(cancellationToken);
            _ = Task.Run(async () =>
            {
                try
                {
                    await RunBookJobAsync(jobId, state, cancellationToken);
                }
                finally
                {
                    _bookSemaphore.Release();
                }
            }, cancellationToken);
        }
    }

    /// <summary>
    ///     Runs one workbook generation job across selected sheets.
    /// </summary>
    private async Task RunBookJobAsync(Guid jobId, JobControlState state, CancellationToken cancellationToken)
    {
        var request = await LoadRequestAsync(jobId, cancellationToken);
        if (request == null) return;

        try
        {
            await UpdateJobStatusAsync(jobId, JobStatusEntity.Running, "Running", cancellationToken);

            Directory.CreateDirectory(request.SaveFolder);
            using var workbook = WorkbookService.OpenWorkbook(request.Book.FilePath);

            var selectedSheets = ValidationService.ResolveSelectedSheets(request);
            var sheetTasks = selectedSheets.Select(async sheetName =>
            {
                await _sheetSemaphore.WaitAsync(cancellationToken);
                try
                {
                    await RunSheetJobAsync(jobId, request, workbook, sheetName, state, cancellationToken);
                }
                finally
                {
                    _sheetSemaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(sheetTasks);

            if (state.IsCancelled)
                await UpdateJobStatusAsync(jobId, JobStatusEntity.Cancelled, "Cancelled", cancellationToken);
            else
                await UpdateJobStatusAsync(jobId, JobStatusEntity.Completed, "Completed", cancellationToken);
        }
        catch (Exception ex)
        {
            await UpdateJobStatusAsync(jobId, JobStatusEntity.Failed, ex.Message, cancellationToken);
        }

        var snapshot = await GetJobAsync(jobId, cancellationToken);
        if (snapshot != null) RaiseJobUpdated(snapshot);
    }

    /// <summary>
    ///     Processes one selected worksheet into an output presentation.
    /// </summary>
    private async Task RunSheetJobAsync(
        Guid jobId,
        SlidesGenerateRequest request,
        IXLWorkbook workbook,
        string sheetName,
        JobControlState control,
        CancellationToken cancellationToken)
    {
        var worksheet =
            workbook.Worksheets.FirstOrDefault(ws =>
                string.Equals(ws.Name, sheetName, StringComparison.OrdinalIgnoreCase));
        if (worksheet == null) throw new InvalidOperationException($"Sheet '{sheetName}' not found.");

        var used = WorksheetService.GetContentRange(worksheet);
        var totalRows = Math.Max(0, (used?.RowCount() ?? 1) - 1);

        var template = request.SheetToSlideMap.TryGetValue(sheetName, out var sheetTemplate)
            ? sheetTemplate
            : throw new InvalidOperationException(
                $"Template configuration is missing for sheet '{sheetName}'.");

        var outputPath = Path.Combine(request.SaveFolder, $"{EscapeFileName(sheetName)}.pptx");
        File.Copy(template.FilePath, outputPath, true);

        await UpdateSheetStateAsync(jobId, sheetName, outputPath, 0, totalRows, JobStatusEntity.Running, null,
            cancellationToken);
        await EnsureRowStatesInitializedAsync(jobId, sheetName, totalRows, cancellationToken);

        using var document = XmlPresentationService.OpenOrCreatePresentation(outputPath);
        var presentationPart = document.PresentationPart
                               ?? throw new InvalidOperationException("Invalid presentation part.");
        var slideId = XmlPresentationService.GetSlideId(document, template.Index - 1)
                      ?? throw new InvalidOperationException(
                          $"Template slide index {template.Index} is invalid.");
        var relationshipId = slideId.RelationshipId?.Value
                             ?? throw new InvalidOperationException("Template slide relationship id is missing.");

        for (var row = 1; row <= totalRows; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (control.IsCancelled) throw new OperationCanceledException("Job cancelled");

            var rowState = await GetRowStateAsync(jobId, sheetName, row, cancellationToken);
            if (rowState == RowProcessState.Completed)
            {
                await UpdateSheetStateAsync(jobId, sheetName, outputPath, row, totalRows, JobStatusEntity.Running,
                    null,
                    cancellationToken);
                continue;
            }

            while (control.IsPaused && !control.IsCancelled)
            {
                await UpdateJobStatusAsync(jobId, JobStatusEntity.Paused, "Paused", cancellationToken);
                await Task.Delay(250, cancellationToken);
            }

            var rowKey = GetRowIdempotencyKey(jobId, sheetName, row);
            await SetRowStateAsync(jobId, sheetName, row, RowProcessState.InProgress, rowKey, null, cancellationToken);

            await _generateService.ProcessRowAsync(
                document,
                relationshipId,
                used,
                row,
                request.TextConfigs,
                request.ImageConfigs,
                cancellationToken);

            await SetRowStateAsync(jobId, sheetName, row, RowProcessState.Completed, rowKey, null, cancellationToken);

            await UpdateSheetStateAsync(jobId, sheetName, outputPath, row, totalRows, JobStatusEntity.Running, null,
                cancellationToken);

            var progress = ComputeProgress(jobId);
            await UpdateJobProgressAsync(jobId, progress, cancellationToken);
        }

        XmlPresentationService.RemoveSlide(document, template.Index);
        document.Save();

        await UpdateSheetStateAsync(jobId, sheetName, outputPath, totalRows, totalRows, JobStatusEntity.Completed,
            null,
            cancellationToken);
    }


    private static string EscapeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(ch => invalid.Contains(ch) ? '_' : ch));
    }

    private static string GetRowIdempotencyKey(Guid jobId, string sheetName, int row)
    {
        return $"{jobId:N}:{sheetName}:{row}";
    }

    private async Task<SlidesGenerateRequest?> LoadRequestAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT request_json FROM jobs WHERE id = $id;";
        command.Parameters.AddWithValue("$id", jobId.ToString());
        var json = await command.ExecuteScalarAsync(cancellationToken) as string;
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<SlidesGenerateRequest>(json);
    }

    private async Task ResumeJobsOnStartupAsync(CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = @"
SELECT id, status
FROM jobs
WHERE status IN ('Pending', 'Running');";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var jobId = Guid.Parse(reader.GetString(0));
            _controlStates.TryAdd(jobId, new JobControlState());
            await _queue.Writer.WriteAsync(jobId, cancellationToken);
        }
    }

    private async Task UpdateJobStatusAsync(Guid jobId, JobStatusEntity status, string? message,
        CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE jobs
SET status = $status,
	updated_at = $updatedAt,
	message = $message
WHERE id = $id;";
        command.Parameters.AddWithValue("$status", status.ToString());
        command.Parameters.AddWithValue("$updatedAt",
            DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$message", message ?? string.Empty);
        command.Parameters.AddWithValue("$id", jobId.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpdateJobProgressAsync(Guid jobId, double progress, CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE jobs
SET progress = $progress,
	updated_at = $updatedAt
WHERE id = $id;";
        command.Parameters.AddWithValue("$progress", Math.Clamp(progress, 0d, 100d));
        command.Parameters.AddWithValue("$updatedAt",
            DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$id", jobId.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);

        var snapshot = await GetJobAsync(jobId, cancellationToken);
        if (snapshot != null) RaiseJobUpdated(snapshot);
    }

    private async Task UpdateSheetStateAsync(Guid jobId, string sheetName, string outputPath, int currentRow,
        int totalRows,
        JobStatusEntity status, string? error, CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE job_sheets
SET output_path = $outputPath,
	current_row = $currentRow,
	total_rows = $totalRows,
	status = $status,
	error = $error
WHERE job_id = $jobId AND sheet_name = $sheetName;";
        command.Parameters.AddWithValue("$outputPath", outputPath);
        command.Parameters.AddWithValue("$currentRow", currentRow);
        command.Parameters.AddWithValue("$totalRows", totalRows);
        command.Parameters.AddWithValue("$status", status.ToString());
        command.Parameters.AddWithValue("$error", (object?)error ?? DBNull.Value);
        command.Parameters.AddWithValue("$jobId", jobId.ToString());
        command.Parameters.AddWithValue("$sheetName", sheetName);
        await command.ExecuteNonQueryAsync(cancellationToken);

        var snapshot = await GetJobAsync(jobId, cancellationToken);
        if (snapshot != null) RaiseJobUpdated(snapshot);
    }

    private async Task EnsureRowStatesInitializedAsync(Guid jobId, string sheetName, int totalRows,
        CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var countCommand = connection.CreateCommand();
        countCommand.CommandText = @"
SELECT COUNT(1)
FROM job_rows
WHERE job_id = $jobId AND sheet_name = $sheetName;";
        countCommand.Parameters.AddWithValue("$jobId", jobId.ToString());
        countCommand.Parameters.AddWithValue("$sheetName", sheetName);
        var count = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken),
            CultureInfo.InvariantCulture);
        if (count > 0) return;

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        for (var row = 1; row <= totalRows; row++)
        {
            var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = @"
INSERT INTO job_rows(job_id, sheet_name, row_index, idempotency_key, status, updated_at, message)
VALUES($jobId, $sheetName, $rowIndex, $key, $status, $updatedAt, NULL);";
            insert.Parameters.AddWithValue("$jobId", jobId.ToString());
            insert.Parameters.AddWithValue("$sheetName", sheetName);
            insert.Parameters.AddWithValue("$rowIndex", row);
            insert.Parameters.AddWithValue("$key", GetRowIdempotencyKey(jobId, sheetName, row));
            insert.Parameters.AddWithValue("$status", nameof(RowProcessState.Pending));
            insert.Parameters.AddWithValue("$updatedAt",
                DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<RowProcessState> GetRowStateAsync(Guid jobId, string sheetName, int row,
        CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = @"
SELECT status
FROM job_rows
WHERE job_id = $jobId AND sheet_name = $sheetName AND row_index = $rowIndex;";
        command.Parameters.AddWithValue("$jobId", jobId.ToString());
        command.Parameters.AddWithValue("$sheetName", sheetName);
        command.Parameters.AddWithValue("$rowIndex", row);
        var value = await command.ExecuteScalarAsync(cancellationToken) as string;

        return Enum.TryParse<RowProcessState>(value, true, out var state)
            ? state
            : RowProcessState.Pending;
    }

    private async Task SetRowStateAsync(Guid jobId, string sheetName, int row, RowProcessState state,
        string idempotencyKey, string? message, CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE job_rows
SET status = $status,
	idempotency_key = $key,
	updated_at = $updatedAt,
	message = $message
WHERE job_id = $jobId
  AND sheet_name = $sheetName
  AND row_index = $rowIndex;";
        command.Parameters.AddWithValue("$status", state.ToString());
        command.Parameters.AddWithValue("$key", idempotencyKey);
        command.Parameters.AddWithValue("$updatedAt",
            DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$message", (object?)message ?? DBNull.Value);
        command.Parameters.AddWithValue("$jobId", jobId.ToString());
        command.Parameters.AddWithValue("$sheetName", sheetName);
        command.Parameters.AddWithValue("$rowIndex", row);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private double ComputeProgress(Guid jobId)
    {
        using var connection = OpenConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COALESCE(SUM(current_row), 0), COALESCE(SUM(total_rows), 0)
FROM job_sheets
WHERE job_id = $jobId;";
        command.Parameters.AddWithValue("$jobId", jobId.ToString());
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return 0d;

        var totalCurrent = reader.GetInt64(0);
        var totalRows = reader.GetInt64(1);
        if (totalRows <= 0) return 0d;
        return totalCurrent * 100d / totalRows;
    }

    private void InitializeDatabase()
    {
        using var connection = OpenConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS jobs(
	id TEXT PRIMARY KEY,
	status TEXT NOT NULL,
	progress REAL NOT NULL,
	created_at TEXT NOT NULL,
	updated_at TEXT NOT NULL,
	request_json TEXT NOT NULL,
	message TEXT NULL
);

CREATE TABLE IF NOT EXISTS job_sheets(
	job_id TEXT NOT NULL,
	sheet_name TEXT NOT NULL,
	output_path TEXT NOT NULL,
	current_row INTEGER NOT NULL,
	total_rows INTEGER NOT NULL,
	status TEXT NOT NULL,
	error TEXT NULL,
	PRIMARY KEY(job_id, sheet_name),
	FOREIGN KEY(job_id) REFERENCES jobs(id)
);

CREATE TABLE IF NOT EXISTS job_rows(
	job_id TEXT NOT NULL,
	sheet_name TEXT NOT NULL,
	row_index INTEGER NOT NULL,
	idempotency_key TEXT NOT NULL,
	status TEXT NOT NULL,
	updated_at TEXT NOT NULL,
	message TEXT NULL,
	PRIMARY KEY(job_id, sheet_name, row_index),
	FOREIGN KEY(job_id, sheet_name) REFERENCES job_sheets(job_id, sheet_name)
);";
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        return new SqliteConnection($"Data Source={_dbPath};Pooling=True;Cache=Shared");
    }

    private void RaiseJobUpdated(JobSnapshotEntity snapshot)
    {
        try
        {
            _ = _snapshotWorkflowDispatcher.PersistAsync(snapshot, CancellationToken.None);
            JobUpdated?.Invoke(snapshot);
        }
        catch
        {
            // TODO: Log
        }
    }
}

#pragma warning restore CS8602