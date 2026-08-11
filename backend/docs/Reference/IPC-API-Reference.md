# IPC API Reference (JSON-RPC 2.0)

SlideGenerator communicates with the frontend sidecar via JSON-RPC 2.0 over standard I/O.

## Transport Configuration

- **Input (stdin)**: Incoming JSON-RPC requests.
- **Output (stdout)**: Outgoing responses and notifications (NDJSON).
- **Error (stderr)**: System logs only.

Framing is NDJSON (`NewLineDelimitedMessageHandler`); serialization uses STJ (`SystemTextJsonFormatter`).

---

## Methods

### Generator — active workflows

| Method                       | Handler                                  | Description                              |
|------------------------------|------------------------------------------|------------------------------------------|
| `generator.active.start`     | `GeneratingActiveHandler.StartAsync`     | Starts a new generation workflow.        |
| `generator.active.cancel`    | `GeneratingActiveHandler.CancelAsync`    | Cancels a single running workflow.       |
| `generator.active.pause`     | `GeneratingActiveHandler.PauseAsync`     | Suspends a single running workflow.      |
| `generator.active.resume`    | `GeneratingActiveHandler.ResumeAsync`    | Resumes a suspended workflow.            |
| `generator.active.cancelAll` | `GeneratingActiveHandler.CancelAllAsync` | Cancels every running workflow.          |
| `generator.active.pauseAll`  | `GeneratingActiveHandler.PauseAllAsync`  | Suspends every running workflow.         |
| `generator.active.list`      | `GeneratingActiveHandler.ListAsync`      | Lists every active workflow.             |

### Generator — completed workflows

| Method                          | Handler                                     |
|---------------------------------|---------------------------------------------|
| `generator.completed.list`      | `GeneratingCompletedHandler.ListAsync`      |
| `generator.completed.delete`    | `GeneratingCompletedHandler.DeleteAsync`    |
| `generator.completed.deleteAll` | `GeneratingCompletedHandler.DeleteAllAsync` |

### Recipe

| Method          | Handler                     |
|-----------------|-----------------------------|
| `recipe.list`   | `RecipeHandler.ListAsync`   |
| `recipe.query`  | `RecipeHandler.QueryAsync`  |
| `recipe.add`    | `RecipeHandler.AddAsync`    |
| `recipe.update` | `RecipeHandler.UpdateAsync` |
| `recipe.delete` | `RecipeHandler.DeleteAsync` |
| `recipe.export` | `RecipeHandler.ExportAsync` |
| `recipe.import` | `RecipeHandler.ImportAsync` |

### Summarization

| Method                       | Handler                                           |
|------------------------------|---------------------------------------------------|
| `summarization.workbook`     | `SummarizationHandler.SummarizeWorkbookAsync`     |
| `summarization.presentation` | `SummarizationHandler.SummarizePresentationAsync` |
| `summarization.recipe`       | `SummarizationHandler.SummarizeRecipeAsync`       |
| `summarization.recipeById`   | `SummarizationHandler.SummarizeRecipeByIdAsync`   |

### Settings

| Method                     | Handler                                |
|----------------------------|----------------------------------------|
| `settings.get`             | `SettingsHandler.GetAsync`             |
| `settings.update`          | `SettingsHandler.UpdateAsync`          |
| `settings.resetToDefaults` | `SettingsHandler.ResetToDefaultsAsync` |

---

## Notifications (Server → Client)

Progress is scoped to Request/Job/Row and coalesced (current-state, last-write-wins) rather than streamed as raw
lifecycle events. `ProgressCoalescer` buffers dirty state and log lines separately, then at most once per second
forwards each non-empty batch. **Every notification's `params` is a single positional array** — i.e. `params[0]` is
the batch list — not a named-object parameter, since the payload is a `List<T>`, not a DTO with properties.

### `progress/request`

Pushed when one or more requests' aggregate lifecycle phase changed since the last flush tick. Array of:

```json
{
  "requestId": "...",
  "phase": "PreparationStarted",
  "timestamp": "..."
}
```

`phase` is `RequestPhase`: `PreparationStarted` | `ProcessingStarted` | `Completed` — monotonically increasing.

### `progress/jobs`

Pushed when one or more jobs' status changed. Array of:

```json
{
  "requestId": "...",
  "jobId": "...",
  "status": "Running",
  "timestamp": "..."
}
```

`status` is `Status`: `Pending` | `Running` | `Complete` | `Paused` | `Cancelled` | `Error`.

### `progress/rows`

Pushed when one or more data rows' status changed. Array of:

```json
{
  "requestId": "...",
  "jobId": "...",
  "rowIndex": 1,
  "status": "Processing",
  "stage": "Downloading",
  "note": "https://drive.google.com/...",
  "timestamp": "..."
}
```

`status` is `RowStatus`: `Waiting` | `Processing` | `Done` | `Error`. `stage` is `RowStage`: `None` | `Downloading` |
`CroppingImage` | `SavingOutput`. `note` is free text (e.g. the URL being downloaded, or a row's failure message) and
may be `null`.

### `log/entries`

Pushed for every log line written since the last flush tick — never coalesced or dropped, unlike the 3 notifications
above. Array of:

```json
{
  "timestamp": "...",
  "path": "requestId/jobId/rowIndex",
  "level": "INF",
  "info": "Resolving URL: https://drive.google.com/..."
}
```

`path` is the Request/Job/Row scope path this line was written under (trailing segments omitted when not
applicable — e.g. a job-level line is just `"requestId/jobId"`), not a physical file path. `level` is a 3-letter
abbreviation (`"VRB"`/`"DBG"`/`"INF"`/`"WRN"`/`"ERR"`/`"FTL"`) matching what's written to the on-disk log file — not
the full Serilog level name.

---

## Serialization Rules

- **Naming**: `camelCase` for all properties (default STJ policy).
- **Enums**: Serialized as **strings** via `JsonStringEnumConverter` (e.g., `"Center"`, `"RuleOfThirds"`).
- **Polymorphism**:
    - `RoiOption` is discriminated by a `"type"` property (`"Center"` | `"RuleOfThirds"`) via `RoiOptionJsonAdapter`.
    - `RectangleF` is serialized as `{ "x", "y", "width", "height" }` via `RectangleFJsonAdapter`.
- **Single-object parameters**: Methods that accept one DTO are registered with
  `UseSingleObjectParameterDeserialization = true` (set through the local `Attr()` helper in `Program.cs`).
