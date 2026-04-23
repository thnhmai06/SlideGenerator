# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Does

SlideGenerator automates PowerPoint generation from templates. Given an Excel workbook and a PowerPoint template, it iterates over worksheet rows and produces one slide per row — replacing Mustache placeholders with text values and image shapes with downloaded/edited images.

## Build Commands

```bash
# Build (solution uses SlideGenerator.slnx)
dotnet build

# Build release
dotnet build -c Release

# Clean
dotnet clean
```

SDK: .NET 10.0 (`global.json` pins to `latestMajor`, prereleases allowed). No test projects currently exist.

## Architecture: Clean Architecture

Three layers with strict dependency rules:

| Layer | Projects | Dependency Rule |
|---|---|---|
| **Domain** | `SlideGenerator.Domain` | No dependencies on Application or Infrastructure |
| **Application** | `SlideGenerator.Application` | Depends on Domain; defines abstractions for Infrastructure |
| **Infrastructure** | `SlideGenerator.Infrastructure` | Implements Application abstractions using external libs |

Elsa Workflows lives exclusively in Infrastructure. Application must not reference Elsa.

### Module Folder Convention

Each module uses consistent subfolders:
- `Abstractions/` — interfaces/contracts (`I`-prefixed)
- `Entities/` — business objects with identity/behavior
- `Models/` — DTOs, value objects, records
- `Rules/` — constants, enums, domain rules, extension mappings
- `Services/` — use-case/module logic
- `Activities/` — Application-layer workflow leaf activities (pure C#, no Elsa)
- `Adapters/` — external library wrappers (Infrastructure only)

## Key Technologies

| Library | Purpose |
|---|---|
| **Elsa Workflows 3.6** | Owned by Infrastructure; Application defines its own DSL (`Activity`, `Sequence`, `ForEach<T>`, `ParallelForEach<T>`, `SlotGated`) |
| **ClosedXML** | Reading Excel workbooks |
| **DocumentFormat.OpenXml** | PowerPoint XML manipulation (never use Spire for slide editing) |
| **OpenCvSharp4 + YuNet** | Face detection for image ROI |
| **Magick.NET-Q8** | Image processing/editing |
| **Downloader** | File download with retry |
| **YamlDotNet** | YAML config deserialization |
| **Stubble.Core** | Mustache template rendering |

## Core Design Patterns

### Registry Pattern (Reference-Counted Resources)
`Registry<TKey, TResource>` base with `FileRegistry<T>`. Always acquire resources via `Acquire()` — auto-disposes when reference count hits zero. Never hold raw file handles across activities.

### Workflow-as-Code (Application DSL)
- Application defines `Activity` / `Sequence` / `ForEach<T>` / `ParallelForEach<T>` / `SlotGated` — no Elsa dependency
- Leaf activities implement `ILeafActivity`; composite iteration types implement `IIterableActivity`
- `GeneratingWorkflow.Build(request)` composes the pipeline using the DSL and closures; state is shared via `WorksheetContext`
- Infrastructure's `ApplicationWorkflowExecutor` interprets the tree; `ElsaGeneratingRunner` implements `IWorkflowRunner<GeneratingRequest>`
- Concurrency gates use `IAsyncKeyedLocker<SlotType>` wrapped by `SlotGated` (Application) and acquired by the executor (Infrastructure)

### Cloud Resolver Chain
`CloudResolversManager` routes cloud storage URLs (Google Drive, Google Photos, OneDrive, SharePoint) to direct download URLs before `DownloadRegistry` fetches them.

## C# Conventions

- **Framework**: `net10.0`, `Nullable: enable`, `ImplicitUsings: enable`, file-scoped namespaces
- **Models**: prefer `record` for data-centric types (identifier, instruction, request)
- **Classes**: prefer `sealed` when inheritance not needed; name concretely (`YamlSerializer`, not `Serializer`)
- **Async**: use `async/await` + `ConfigureAwait(false)` in service/library code
- **Null safety**: validate inputs early; prefer `TryGet`/`Try...` patterns over throwing for expected failures
- **Concurrency**: use `ConcurrentDictionary`/locks for shared state; clean up registries on completion
- **XML docs**: required for all public APIs (summary, param, returns, remarks)
- StyleCop.Analyzers is applied solution-wide; SA1402 (one type per file) is set to suggestion

## Workflow: GeneratingWorkflow

**Input**: `GeneratingRequest` — `Graph` (worksheet→slide template mapping), `TextInstructions`, `ImageInstructions`, `SaveFolder`

**Pipeline per worksheet**:
1. Open workbook; skip if worksheet/book missing
2. Clone template to a working copy; strip to single template slide; scan for placeholders (`X`)
3. Specialize instructions against the worksheet's used range (discard columns not present)
4. For each data row (parallel, throttled by slot limit in Settings):
   - Resolve cloud URLs → save resolved URLs to variable/output (idempotent across restarts)
   - Download images via `DownloadRegistry`
   - Edit images (TODO stubs exist)
   - Clone template slide; replace text (Mustache) and images; maintain row order after template removal
5. Save output file with normalized name + extension from Settings
6. Close workbook/presentation when no longer needed

**Slide editing must go through XML (`DocumentFormat.OpenXml`), never Spire.**

## Dependency Checklist (Self-Check Before Committing)

- Does Domain reference Application or Infrastructure? → **must not**
- Are business rules in Infrastructure? → **must not be**
- Does Application call concrete implementations directly instead of abstractions? → **review required**
- Is a new library added to the correct layer only? → Domain must stay framework-free
