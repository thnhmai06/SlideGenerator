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
| `generator.active.query`     | `GeneratingActiveHandler.QueryAsync`     | Returns details for a specific workflow. |

### Generator — completed workflows

| Method                          | Handler                                     |
|---------------------------------|---------------------------------------------|
| `generator.completed.list`      | `GeneratingCompletedHandler.ListAsync`      |
| `generator.completed.query`     | `GeneratingCompletedHandler.QueryAsync`     |
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

### `workflow/progress`

Pushed by `WorkflowProgressObserver` whenever `IGeneratingEventBus` emits a step or lifecycle event.

```json
{
  "workflowInstanceId": "...",
  "event": "StepCompleted",
  "phase": "PhaseB",
  "status": "Running",
  "timestamp": "..."
}
```

- `event` corresponds to `GeneratingEvent` (e.g. `StepCompleted`, `WorkflowCompleted`, `WorkflowError`).
- `phase` corresponds to `GeneratingPhase` (`PhaseA` | `PhaseB` | `PhaseC`) when applicable.
- `status` corresponds to `GeneratingStatus`.

---

## Serialization Rules

- **Naming**: `camelCase` for all properties (default STJ policy).
- **Enums**: Serialized as **strings** via `JsonStringEnumConverter` (e.g., `"Center"`, `"RuleOfThirds"`).
- **Polymorphism**:
    - `RoiOption` is discriminated by a `"type"` property (`"Center"` | `"RuleOfThirds"`) via `RoiOptionJsonAdapter`.
    - `RectangleF` is serialized as `{ "x", "y", "width", "height" }` via `RectangleFJsonAdapter`.
- **Single-object parameters**: Methods that accept one DTO are registered with
  `UseSingleObjectParameterDeserialization = true` (set through the local `Attr()` helper in `Program.cs`).
