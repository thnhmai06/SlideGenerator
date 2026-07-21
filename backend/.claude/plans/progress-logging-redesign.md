# Redesign Progress + Logging cho Generator

> **Trạng thái: ĐANG LÀM VIỆC, CHƯA XONG.** Phần thiết kế + implementation ban đầu (mục I–VI dưới đây) đã
> code xong, build/test xanh. Nhưng còn việc chưa chốt/chưa làm (xem mục "Việc còn lại" ở cuối file) — đừng
> coi đây là task đã hoàn tất.

## Context

Cơ chế Progress hiện tại (`Domain/Models/Data/Progress.cs`) là một record `Progress` phẳng, đa mục đích: vừa
báo lifecycle job (`JobStarted`/`JobCompleted`/...), vừa báo step hoàn tất (`StepCompleted`), vừa báo sub-step
granular (`StageReported`). Field `Stage`/`Detail` bị dùng chung cho cả job-level lẫn row-level, không có khái
niệm Row nào cả — FE chỉ nhận được chuỗi tự do `"{N}/{M}"` làm `Detail` để suy luận tiến độ theo dòng dữ liệu.
Không có state nào được lưu lại: `GeneratingEventBus` chỉ là pub/sub thuần, một subscriber attach trễ sẽ mất
mọi event đã bắn trước đó — `ListActiveAsync`/`ListCompletedAsync` phải tự suy luận `Status` từ WorkflowCore
(`DeriveStatus`) mà hoàn toàn không biết job đang ở `Stage`/row nào.

Logging module (`SlideGenerator.Logging`) cũng không có cơ chế intercept: không `ILogEventSink`, không
`LogContext.PushProperty` nào tồn tại — "Scope" hiện tại chỉ là 1 string tĩnh gắn 1 lần khi tạo file logger
(`"Workflow/{RequestName}"`), và 1 file log dùng chung cho *toàn bộ Request* (nhiều Job ghi chung 1 file).

Mục tiêu: thay bằng 3 scope rõ ràng — **Request / Job / Row** — cho cả Progress (trạng thái hiện tại,
coalescible) và Log (append-only, không được mất dòng), thông báo tới FE qua JSON-RPC notification, có state
persist để `Summary` trả về đầy đủ, và tối ưu ghi DB bằng WAL + batch transaction (tối thiểu 1s giữa các lần
gửi).

**Nguyên tắc cốt lõi (đã thống nhất với user):**
- Progress = current-state, **UPSERT**, coalescible (row đổi trạng thái nhiều lần trong 1s → chỉ cần thấy
  trạng thái cuối).
- Log = append-only, **INSERT/buffer**, không được coalesce/mất dòng.
- Cả hai dùng chung một nhịp flush ≥1s, nhưng là hai buffer riêng biệt trong cùng một bộ đếm giờ.
- `Summary` (được `ListActiveAsync`/`ListCompletedAsync` — FE poll định kỳ) trả **đầy đủ** Row + Log mỗi scope
  (không tách API riêng).
- Log **không** có bảng DB riêng — vẫn ghi vào file `.log` hiện có (1 file/Request, giữ nguyên), chỉ thêm
  structured scope (RequestId/JobId/RowIndex) vào từng dòng để lọc theo scope khi cần.
- Trường `Path` trong log notification = **scope path** (`<requestId>/<jobId>/<rowIndex>`), không phải physical
  file path.
- `Summary` đọc log **trực tiếp từ file trên đĩa** mỗi lần `ListActiveAsync`/`ListCompletedAsync` — **không**
  dùng RAM cache tích lũy (user bác bỏ rõ ràng vì rủi ro tràn RAM với job chạy lâu/nhiều dòng log; tần suất
  poll của FE thấp nên chấp nhận chi phí quét file).

## Quyết định thiết kế đã duyệt (6 mục, itemized feedback của user)

1. **RowStage rút gọn còn 3 giá trị** (`None, Downloading, CroppingImage, SavingOutput`), bỏ `ResolvingUrl` —
   xảy ra 1 lần cho toàn bộ URL của cả job (không có quan hệ 1:1 với 1 row), giữ làm log line thường ở Job
   scope.
2. **4 Stage job-level cũ** (`OpeningWorkbook`, `OpeningPresentation`, `LoadingTemplate`, `CleaningUpOutput`)
   không còn là Progress notification — chuyển thành log line thường (Info level) ở Job scope, vì `JobProgress`
   chỉ có `Status` (không có `Stage`).
3. **Rà lại toàn bộ chỗ catch exception liên quan per-row processing** — quyết định log/throw từng chỗ:
   - `EnsureDownloadedAsync`/`CropToPngAsync` (`GenerateJobStep.Image.cs`) — giữ nguyên best-effort per-shape,
     không throw (đã log Warning từ trước).
   - `PreflightCleanup.cs` — giữ nguyên, job-level không phải per-row.
   - **Mới**: vòng lặp `foreach (var dataRow in dataRows)` (`GenerateJobStep.cs`) thêm try/catch quanh
     `GenerateRowSlideAsync`: `LogError` + `_progress.ReportRow(..., RowStatus.Error, note: ex.Message)` +
     `throw;` (rethrow nguyên vẹn, không tự ý nuốt lỗi).
4. **Pre-seed toàn bộ row là `Waiting`** khi job bắt đầu Generation — 1 batch UPSERT duy nhất
   (`StepProgress.SeedRows`).
5. **`RequestProgress.Phase` suy luận tổng hợp** từ nhiều Job chạy song song, qua
   `ProgressCoalescer.TrackRequestAggregate` + `AnnounceExpectedJobCount` (tránh race giữa spawn loop đồng bộ
   và WorkflowCore's async lifecycle poller).
6. **Log đọc thẳng từ file trên đĩa** cho `Summary`, không RAM cache (xem "Nguyên tắc cốt lõi" ở trên).

## I. Data model (`Domain/Models/Data/Progress.cs`, `Summary.cs`)

```csharp
public enum RequestPhase { PreparationStarted, ProcessingStarted, Completed }

public sealed record RequestProgress
{
    public required string RequestId { get; init; }
    public required RequestPhase Phase { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed record JobProgress
{
    public required string RequestId { get; init; }
    public required string JobId { get; init; }
    public required Status Status { get; init; }   // + thêm Pending
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed record RowProgress
{
    public required string RequestId { get; init; }
    public required string JobId { get; init; }
    public required int RowIndex { get; init; }
    public required RowStatus Status { get; init; }
    public RowStage Stage { get; init; } = RowStage.None;
    public string? Note { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
```

- `Enum/Status.cs` — thêm `Pending`.
- `Enum/RowStatus.cs` (mới) — `Waiting, Processing, Done, Error`.
- `Enum/RowStage.cs` (mới, thay `Stage.cs` cũ đã xóa) — `None, Downloading, CroppingImage, SavingOutput`.
- `Summary.cs` mở rộng: `Summary.Phase`/`Summary.Logs`/`Summary.Jobs[JobSummary]`;
  `JobSummary.Rows[RowSummary]`/`JobSummary.Logs`; `RowSummary` (Status/Stage/Note/Timestamp/Logs); `LogEntry`
  (Timestamp/Path/Level/Info — Level là `string` abbreviation "INF"/"WRN"/... để khớp format file log).

`IEventBus` — 3 overload `Publish(RequestProgress|JobProgress|RowProgress)` + `AnnounceExpectedJobCount`.

`StepProgress` — bỏ `Report(Stage, detail)` cũ, thêm `ReportRow(rowIndex, status, stage, note)` +
`SeedRows(rowIndices)`.

## II. Studio.db (Requests / Jobs / Rows) — persist Progress

`NameAndPaths.DataFolder.StudioFile` (theo khuôn `WorkflowsFile`/`RecipesFile`/`CacheFile`) — WAL mode,
`busy_timeout=5000`. 3 bảng `Requests`/`Jobs`/`Rows`, PK ghép, UPSERT qua `ON CONFLICT DO UPDATE`.

`IStudioRepository`/`StudioRepository` (Dapper, short-lived connection/op — pattern giống `SqliteCache`).
`Service.DeleteAsync`/`DeleteAllCompletedAsync` xóa kèm dòng Studio.db tương ứng.

## III/IV. ProgressCoalescer — cầu nối Publish → DB (upsert) → JSON-RPC notify

`Stdio/Implementations/ProgressCoalescer.cs` (thay `WorkflowProgressObserver.cs`):
- Buffer coalesce (`ConcurrentDictionary`, last-write-wins) cho Request/Job/Row; buffer append-only
  (`ConcurrentQueue`) cho Log — không bao giờ mất dòng.
- `PeriodicTimer` 1s: mỗi tick, upsert dirty vào `IStudioRepository` rồi gửi
  `progress/request`/`progress/jobs`/`progress/rows`/`log/entries` qua JSON-RPC — 1 lần/tick, gộp toàn bộ thay
  đổi kể từ tick trước.
- Try/catch **trong từng tick** (không chỉ quanh cả loop) — 1 lần flush lỗi không được chặn vĩnh viễn các lần
  sau.
- `DetachAsync()` flush cuối cùng trước khi hủy timer — tránh mất batch <1s cuối khi shutdown.
- `RequestPhase` aggregation: `RequestAggregateState` (ExpectedJobCount/StartedJobs/TerminalJobs) theo dõi
  per-request, dùng `ExpectedJobCount` (announce từ `Service.CreateAsync` trước spawn loop) làm mẫu số —
  tránh race giữa spawn loop đồng bộ và WorkflowCore's lifecycle poller bất đồng bộ.

**Bug thật đã tìm và fix qua E2E test** (`ProgressCoalescerTests.cs`, dùng `FullDuplexStream.CreatePair()` +
`JsonRpc` thật, không mock): `NotifyAsync<T>` ban đầu dùng `_jsonRpc.NotifyWithParameterObjectAsync(method,
payload)` — marshal theo named-parameter/reflection-over-property convention, với `List<T>`/`IReadOnlyList<T>`
(không có property nào) sẽ serialize thành `"params": {}` rỗng thay vì mảng thật. Nghĩa là **mọi notification
Progress/Log gửi ra production đều sẽ là payload rỗng — tính năng hoàn toàn hỏng**, không phát hiện được bằng
test mock (NSubstitute không serialize gì cả). Fix: đổi sang `_jsonRpc.NotifyAsync(method, payload)` (positional
single-argument binding → `"params": [[...]]`).

## V. Logging: sink mới + LogContext scoping (không thêm bảng DB)

- `IFileLoggerFactory.CreateFile` đổi chữ ký thêm callback `Action<LogNotification>? onLogEvent` +
  `IReadOnlyList<string>? scopePropertyNames` (xem "Fix sau plan" bên dưới — ban đầu chỉ có callback, sau đó
  sửa thêm tham số scope).
- `ScopeNotifyingSink` (`Serilog.Core.ILogEventSink`) — build `Path` từ scope properties hiện có trên
  `LogEvent` (`LogContext.PushProperty`), gọi callback `onLogEvent`.
- `FileLogFormatter` đổi format dòng ghi để chứa scope path parse lại được:
  `[{loggerName}/{path}] {levelAbbr}: {message}`.
- `LogContext.PushProperty("RequestId"/"JobId"/"RowIndex", ...)` thêm ở `GenerateJobStep.cs`,
  `InspectUrlsStep.cs`, `PreflightCleanup.cs` (đầu `Run`/`RunAsync`), `GenerateJobStep.Row.cs`
  (`GenerateRowSlideAsync`, thêm `RowIndex`).
- `ILogNotifier` (Generator, interface, `dep-interface-ownership` pattern giống `IEventBus`) —
  `Middleware.cs` inject, truyền callback xuống `CreateFile`. `LogNotifier` (Stdio, implementation) — sống
  cạnh `ProgressCoalescer`, gộp vào cùng buffer `ConcurrentQueue<LogEntry>` để drain mỗi tick.
- `LogFileReader` (Generator, mới) — đọc file `.log` trên đĩa, parse regex theo format của
  `FileLogFormatter`, lọc theo scope path cho `Summary.Logs`/`JobSummary.Logs`/`RowSummary.Logs`. Đây là I/O
  đồng bộ mỗi lần poll — chấp nhận được vì FE không poll dồn dập.

## VI. Wiring (Stdio)

- `GeneratingEventBus.cs` — 3 event `OnRequestProgress`/`OnJobProgress`/`OnRowProgress` + `OnExpectedJobCount`.
- `WorkflowProgressObserver.cs` → `ProgressCoalescer.cs`.
- `JsonRpcBootstrap.cs` — `AttachProgressObserver` → `AttachProgressCoalescer`, notification method names mới:
  `progress/request`, `progress/jobs`, `progress/rows`, `log/entries` (thay `workflow/progress` cũ).
- `Registration.cs` (Stdio) đăng ký `ProgressCoalescer`/`LogNotifier` singleton thay `WorkflowProgressObserver`.

## Fix sau plan (feedback thêm của user, đã áp dụng sau khi implementation ban đầu xong)

1. **`BuildScopePath` vi phạm dependency direction** — Logging (Foundation module) đang hardcode
   `"RequestId"/"JobId"/"RowIndex"` (business concept của Generator) ngay trong `Utilities.cs`. Fix: đổi
   `BuildScopePath()` thành generic `BuildScopePath(IReadOnlyList<string> propertyNames)` — Logging chỉ join
   scalar property theo tên được truyền vào, không tự biết ý nghĩa scope. `IFileLoggerFactory.CreateFile`,
   `SerilogFileLoggerFactory`, `FileLogFormatter`, `ScopeNotifyingSink` đều nhận thêm tham số
   `scopePropertyNames` (mặc định null/empty → path rỗng). `Middleware.cs` (Generator, nơi sở hữu business
   rule) tự khai báo `ScopePropertyNames = ["RequestId", "JobId", "RowIndex"]` và truyền xuống khi gọi
   `CreateFile`. `Program.cs` (bootstrap system logger, không thuộc per-request scope) truyền `[]`.
2. **`LogNotification.Level` đổi kiểu từ `string` sang `Serilog.Events.LogEventLevel`** — `ScopeNotifyingSink`
   bỏ switch-abbreviation, gán thẳng `logEvent.Level`. `Middleware.cs` (boundary Generator↔Logging, anti-
   corruption layer) tự làm switch abbreviation khi map `LogNotification` (Logging) → `LogEntry` (Generator,
   vẫn giữ `Level: string` để khớp format 3 ký tự đọc từ file của `LogFileReader`).
   `SlideGenerator.Generator.csproj`/`SlideGenerator.Generator.Tests.csproj` phải thêm
   `PackageReference Serilog` — transitive package không flow qua `ProjectReference` do
   `Directory.Build.props` set `PrivateAssets="all"` toàn cục (đúng "NuGet transitivity pitfall" đã ghi trong
   CLAUDE.md).

## Xác minh đã chạy

1. `dotnet build SlideGenerator.slnx` — biên dịch sạch.
2. `dotnet test SlideGenerator.slnx` — toàn bộ suite xanh, trừ 2 test zip-slip cũ của
   `RecipeRepositorySecurityTests` (`Import_BuildPathMappings` NullReferenceException) — xác nhận qua
   `git status`/`git blame` đây là lỗi **pre-existing**, không do session này gây ra, để nguyên theo nguyên
   tắc surgical changes.
3. Chưa test thủ công qua Stdio sidecar thật (JSON-RPC end-to-end với FE) — chỉ có E2E test nội bộ
   (`ProgressCoalescerTests.cs`) giả lập JsonRpc qua `FullDuplexStream`, chưa chạy với FE thật.

## Việc còn lại (chưa làm, chưa được user xác nhận)

- **CLAUDE.md + docs/** (`docs/Reference/IPC-API-Reference.md`,
  `docs/Reference/Modules/Generator.md`/`Logging.md`/`Stdio.md`, `docs/Architecture/System-Overview.md`,
  `docs/Architecture/Workflow-Engine.md`) vẫn mô tả hệ thống `Progress`/`Event`/`ProgressMiddleware`/
  `workflow/progress` cũ — **chưa cập nhật**, đã đề xuất với user nhưng chưa được yêu cầu làm.
- **Xác minh wire-format với FE**: notification giờ gửi theo positional array param
  (`_jsonRpc.NotifyAsync(method, payload)` → `"params": [[...]]`) thay vì named-object cũ — FE cần đọc
  `params[0]`, chưa verify được vì không truy cập được code FE.
- `ProgressCoalescer.RequestAggregateState` (ExpectedJobCount/StartedJobs/TerminalJobs) chỉ sống trong RAM của
  process Stdio — restart giữa chừng 1 request sẽ mất state suy luận `Phase` real-time (dù `Studio.db` vẫn giữ
  `Phase` đã persist cuối cùng nên `Summary` không sai, chỉ real-time inference sau restart có thể lệch).
- Chưa test thủ công end-to-end qua sidecar thật với FE (chỉ có E2E test giả lập nội bộ).
- Working tree hiện tại còn lẫn nhiều thay đổi lớn khác **không thuộc phạm vi task này** (Cloud, Recipe,
  Settings, Document, Image, xóa Coordinator, ...) — đã có sẵn từ trước khi phiên làm việc này bắt đầu, user
  xác nhận commit gộp chung nhưng đây vẫn là phần việc của luồng công việc khác, không phải kết quả của task
  Progress/Logging redesign này.
