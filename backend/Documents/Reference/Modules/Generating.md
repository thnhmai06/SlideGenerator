# Generating Module

The **SlideGenerator.Generating** module is the central orchestration engine. It uses **WorkflowCore** to execute the generation pipeline.

## Responsibility
- Coordinates multi-phase workflows.
- Manages state persistence via SQLite.
- Handles error resilience and partial success.

## The 3-Phase Workflow

### Phase A: Validation & Setup
- **Steps**: `ValidateRequest`, `CreateTemplate`.
- **Logic**: Validates paths, clones the template file, and isolates the specific template slide.

### Phase B: Resource Preparation
- **Steps**: `ExtractData`, `DownloadImage`, `EditImage`.
- **Logic**: 
  - `ExtractData` opens Excel and maps placeholders to row data.
  - `DownloadImage` and `EditImage` run in parallel using WorkflowCore `.ForEach()`.
  - Uses `GateLocker` to throttle concurrent downloads and MagickImage processing.

### Phase C: Assembly & Cleanup
- **Steps**: `ReplaceSlideData`, `CloseAllHandles`.
- **Logic**: 
  - Iterates through `SlideContexts` to perform text and image replacements in the output PowerPoint.
  - Closes all open Syncfusion file handles.

## State Management (`GeneratingContext`)
The workflow state is stored in a single `GeneratingContext` object.
- **Persistence**: Automatically serialized to `Workflows.db`.
- **Ignores**: Fields like `IAppLogger` or open file handles are marked `[JsonIgnore]` and reopened lazily after a process restart.
